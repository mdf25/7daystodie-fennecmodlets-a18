using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;

/**
 *  Tile entity class for block transformer. Takes input items and changes them into output items. 
 */
public class TileEntityBlockTransformer : TileEntityLootContainer
{
    /**
     * Loads the tile entity
     */

    public TileEntityBlockTransformer(Chunk _chunk) : base(_chunk)
    {
        this.tQueue  = new TransformationQueue();
        this.useHash = true;
        Log.Out("Tile Entity created.");
        foreach (KeyValuePair<string, TransformationData> entry in TransformationData.hashmap)
        {
            Log.Out("Hash: " + entry.Key);
            Log.Out("Data: " + entry.Value.ToString());
        }
        Log.Out("All hashes read.");
    }


    /**
     * Loads the tile entity by copying another.
     */

    private TileEntityBlockTransformer(TileEntityBlockTransformer _other) : base(null)
    {
        this.lootListIndex      = _other.lootListIndex;
        this.containerSize      = _other.containerSize;
        this.items              = ItemStack.Clone(_other.items);
        this.bTouched           = _other.bTouched;
        this.worldTimeTouched   = _other.worldTimeTouched;
        this.bPlayerBackpack    = _other.bPlayerBackpack;
        this.bPlayerStorage     = _other.bPlayerStorage;
        this.bUserAccessing     = _other.bUserAccessing;
        this.collection         = _other.collection;
        this.tQueue             = _other.tQueue;
        this.useHash            = _other.useHash;
        this.requiresPower      = _other.requiresPower;
        this.requiresHeat       = _other.requiresHeat;
        this.powerSources       = _other.powerSources;
        this.heatSources        = _other.heatSources;
    }


    /**
     * Sets the above method public
     */

    public new TileEntityBlockTransformer Clone()
    {
        return new TileEntityBlockTransformer(this);
    }


    /**
     * Copies properties from another tile entity into this.
     */

    public void CopyLootContainerDataFromOther(TileEntityBlockTransformer _other)
    {
        this.lootListIndex      = _other.lootListIndex;
        this.containerSize      = _other.containerSize;
        this.items              = ItemStack.Clone(_other.items);
        this.bTouched           = _other.bTouched;
        this.worldTimeTouched   = _other.worldTimeTouched;
        this.bPlayerBackpack    = _other.bPlayerBackpack;
        this.bPlayerStorage     = _other.bPlayerStorage;
        this.bUserAccessing     = _other.bUserAccessing;
        this.collection         = _other.collection;
        this.tQueue             = _other.tQueue;
        this.useHash            = _other.useHash;
        this.requiresPower      = _other.requiresPower;
        this.requiresHeat       = _other.requiresHeat;
        this.powerSources       = _other.powerSources;
        this.heatSources        = _other.heatSources;
    }


    /**
     * Each game tick, this method is called.
     */

    public override void UpdateTick(World world)
    {
        if (!this.UpdateCanHappen(world))
        {
            return;
        }

        this.HandleAddingToQueue();
        this.HandleProcessingQueue();
    }
    

    /**
     * Deals with inserting items into the queue and removing them from the block inventory.
     */

    public void HandleAddingToQueue()
    {
        foreach (TransformationData tData in this.collection.collection)
        {
            ulong transformationTime;
            List<Tuple<int, ItemStack>> locations;
            TransformationJob job;
            if (!this.QueueReadyItems(tData, out transformationTime, out locations, out job))
            {
                continue;
            }

            if (job == null)
            {
                continue;
            }

            ItemStack[] previousState;
            if (!this.StoreReadyItems(locations, out previousState))
            {
                this.RestorePreviousState(previousState);
                this.tQueue.RemoveEntry(job);
            }
        }
    }


    /**
     * Retrieves jobs from the queue and processes them.
     */

    private void HandleProcessingQueue()
    {
        if (!this.tQueue.QueueDefinedAndNotEmpty())
        {
            return;
        }

        List<TransformationJob> jobs = this.tQueue.GetNextTransformations();
        foreach (TransformationJob job in jobs)
        {
            ItemStack[] previousState;
            bool processed = this.ProcessQueueJob(job, out previousState);
            if (processed)
            {
                this.tQueue.RemoveEntry(job);
                continue;
            }
            job.MarkJobNotInProgress();
            this.RestorePreviousState(previousState);
        }
    }


    /**
     * Checks that an update can happen.
     */

    private bool UpdateCanHappen(World world)
    {
        return (this.IsPowered() & this.IsHeated() & (this.tQueue.QueueDefinedAndNotEmpty() | !this.IsEmpty()));
    }


    /**
     * Checks if block is powered. It is required to be next to an electric wire relay.
     */

    private bool IsPowered()
    {
        if (!this.requiresPower)
        {
            return true;
        }

        World world             = GameManager.Instance.World;
        Vector3i tileEntityPos  = this.ToWorldPos();
        List<Vector3i> nearby   = CoordinateHelper.GetCoOrdinatesAround(tileEntityPos, true, 1, 1, 1);

        if (nearby.Count == 0)
        {
            return false;
        }

        Dictionary<Vector3i, TileEntity> tileEntitiesNearby = CoordinateHelper.GetTileEntitiesInCoordinates(world, nearby, TileEntityType.Powered);
        
        if (tileEntitiesNearby.Count == 0)
        {
            return false;
        }

        foreach (KeyValuePair<Vector3i, TileEntity> entry in tileEntitiesNearby)
        {
            TileEntityPowered otherTileEntity   = entry.Value as TileEntityPowered;
            Vector3i otherTileEntityPos         = entry.Key;

            foreach (string powerSource in this.powerSources)
            {
                string name = CoordinateHelper.GetBlockNameAtCoordinate(world, otherTileEntityPos);
                if (!CoordinateHelper.BlockAtCoordinateIs(world, otherTileEntityPos, powerSource))
                {
                    continue;
                }
                
                if (otherTileEntity.IsPowered)
                {
                    return true;
                }
            }
        }
        return false;
    }


    /**
     * Returns true if a heat source is under the block
     */

    private bool IsHeated()
    {
        if (!this.requiresHeat)
        {
            return true;
        }

        World world             = GameManager.Instance.World;
        Vector3i tileEntityPos  = this.ToWorldPos();
        Vector3i posUnderTE     = CoordinateHelper.GetCoordinateBelow(tileEntityPos);

        // If TE is on very bottom of world this will prevent out of boundary shenanigans.
        if (tileEntityPos == posUnderTE)
        {
            return false;
        }

        Dictionary<Vector3i, TileEntity> tileEntitiesUnderneath = CoordinateHelper.GetTileEntitiesInCoordinates(world, new List<Vector3i>() { posUnderTE }, TileEntityType.Workstation);

        if (tileEntitiesUnderneath.Count == 0)
        {
            return false;
        }

        foreach (KeyValuePair<Vector3i, TileEntity> entry in tileEntitiesUnderneath)
        {
            TileEntityWorkstation otherTileEntity = entry.Value as TileEntityWorkstation;
            Vector3i otherTileEntityPos = entry.Key;

            foreach (string heatSource in this.heatSources)
            {
                if (!CoordinateHelper.BlockAtCoordinateIs(world, otherTileEntityPos, heatSource))
                {
                    continue;
                }

                // Checks that there is fuel being used for the workstation as well as if it's burning.
                foreach (ItemStack itemStack in otherTileEntity.Fuel)
                {
                    if (itemStack != null & !itemStack.Equals(ItemStack.Empty.Clone()))
                    {
                        if (otherTileEntity.IsActive(world))
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }


    /**
     * Queues ready transformations.
     */

    private bool QueueReadyItems(TransformationData tData, out ulong transformationTime, out List<Tuple<int, ItemStack>>stackLocations, out TransformationJob job)
    {
        transformationTime = 0;
        job = null;
        if (!tData.HasAllInputs(this.GetItemStacksFromFilledSlots(), out stackLocations))
        {
            return false;
        }
        return this.AddToQueue(tData, out transformationTime, out job);
    }


    /**
     * Removes each item from the inventory and then returns true if all were successfully removed.
     * Outputs the original state of the container in case of failure for safe reverse.
     */

    private bool StoreReadyItems(List<Tuple<int, ItemStack>> locations, out ItemStack[] previousState)
    {
        previousState   = (ItemStack[])this.items.Clone();
        foreach (Tuple<int, ItemStack> entry in locations)
        {
            if (!this.RemoveItemsInSlot(entry.Item1, entry.Item2.count))
            {
                return false;
            }
        }
        return true;
    }
   

    /**
     * Adds to the queue, giving out the transformation time and the job added.
     */

    private bool AddToQueue(TransformationData tData, out ulong time, out TransformationJob job)
    {
        time = 0;
        job = null;
        bool added = this.tQueue.Add(tData, out time, out job);
        return added;
    }


    /**
     * Processes a queue job.
     */

    private bool ProcessQueueJob(TransformationJob job, out ItemStack[] previousState)
    {
        previousState = (ItemStack[])this.items.Clone();
        List<ItemStack> outputs = job.GetTransformationData().GetAllOutputsAfterProbabilityCalculation();
        List<int> assignedSlots = new List<int>();
        List<Tuple<int, ItemStack>> stacksToAdd = new List<Tuple<int, ItemStack>>();
        bool canAddAll = true;

        // If there are no ouputs after probability calculation, then congrats, nothing to do, success.
        if (outputs.Count == 0)
        {
            return true;
        }

        foreach (ItemStack output in outputs)
        {
            List<Tuple<int, ItemStack>> whereToAdd;
            if (!this.RoomInSlotsFor(output, ref assignedSlots, out whereToAdd))
            {
                canAddAll = false;
                return false;
            }

            stacksToAdd.AddRange(whereToAdd);
        }
        
        if (canAddAll)
        {
            foreach (Tuple<int, ItemStack> positionAndStack in stacksToAdd)
            {
                if (!this.TryAddItemToSlot(positionAndStack.Item1, positionAndStack.Item2))
                {
                    return false;
                }
            }
        }

        return canAddAll;
    }


    /**
     * Restores the previous state of the items before removal in case of failure.
     */

    private void RestorePreviousState(ItemStack[] previousState)
    {
        this.items = ItemStack.Clone(previousState, 0, this.items.Length);
        this.tileEntityChanged();
    }


    /**
     * Checks that the slot contains the correct item stack.
     */

    private bool SlotHasCorrectItem(int slot, ItemStack itemStack)
    {
        return (this.StacksHaveSameItem(items[slot], itemStack));
    }


    /**
     * Remove a number {remove} of items from the item stack at slot {slot} if possible. Returns whether items were removed successfully or not.
     */

    private bool RemoveItemsInSlot(int slot, int remove)
    {
        if (!this.CanRemoveItemsInSlot(slot, remove))
        {
            return false;
        }

        ItemStack newStack  = new ItemStack(this.items[slot].itemValue, this.items[slot].count - remove);
        this.items[slot]    = newStack.Clone();

        if (items[slot].count <= 0)
        {
            items[slot].Clear();
        }
        this.tileEntityChanged();
        return true;
    }


    /**
     * Checks that we can remove the item in slot {slot}
     */

    private bool CanRemoveItemsInSlot(int slot, int remove)
    {
        // If slot is not defined, return false
        if (slot > this.items.Length - 1)
        {   
            return false;
        }

        // If we have no itemstack defined, terminate to avoid nullref.
        if (this.items[slot] == null)
        {
            Log.Warning("Item slot " + slot.ToString() + " is null");
            return false;
        }

        // If the itemstack is empty, terminate.
        if (this.items[slot].IsEmpty())
        {
            Log.Warning("Item slot " + slot.ToString() + " is empty.");
            return false;
        }

        // If the number to remove is not 
        if (remove > items[slot].count)
        {
            Log.Warning("Item slot " + slot.ToString() + " doesn't have enough items.");
            return false;
        }
        return true;
    }
    

    /**
     * Returns whether there is room for the items in slot. 
     * Outputs how many spaces are left if the slot were to have the items added.
     * Outputs how many spaces are needed if the item cannot fill the slot completely.
     */

    private bool FilledSlotHasRoomFor(int slot, ItemStack itemStack, out int spacesLeft, out int spacesNeeded, out bool matchingStack)
    {
        if (!this.SlotHasCorrectItem(slot, itemStack))
        {
            spacesLeft = spacesNeeded = 0;
            return matchingStack = false;
        }

        matchingStack        = true;

        int stackNumber = this.TotalStackSizeFor(itemStack);
        int numAlready  = this.GetNumItemsInSlot(slot);
        int numToAdd    = itemStack.count;
        
        spacesLeft      = Mathf.Max(stackNumber - numAlready, 0);
        spacesNeeded    = Mathf.Max(numAlready + numToAdd - stackNumber, 0);

        return (spacesLeft >= 0 && spacesNeeded == 0);
    }
    

    /**
     * Returns whether an empty slot has room to stack something completely.
     * Outputs the number of spaces in the slot
     * Outputs the number of spaces needed, if the stack can't completely fill the slot. 
     */

    private bool EmptySlotHasRoomFor(int slot, ItemStack itemStack, out int spacesLeft, out int spacesNeeded)
    {
        if (!this.SlotIsEmpty(slot))
        {
            spacesLeft = spacesNeeded = 0;
            return false;
        }

        int stackNumber = this.TotalStackSizeFor(itemStack);
        int numToAdd    = itemStack.count;

        spacesLeft = Mathf.Max(stackNumber, 0);
        spacesNeeded = Mathf.Max(numToAdd - stackNumber, 0);

        return (spacesLeft >= 0 && spacesNeeded == 0);
    }
    

    /**
     * Gets how many items are in a slot
     */

    private int GetNumItemsInSlot(int slot)
    {
        if (this.items[slot].itemValue.type == 0)
        {
            return 0;
        }
        return items[slot].count;
    }


    /**
     * Gets the stack number for an item stack.
     */

    private int TotalStackSizeFor(ItemStack itemStack)
    {
        return ItemClass.GetForId(itemStack.itemValue.type).Stacknumber.Value;
    }


    /**
     * Tries to add an item to a slot.
     */

    private bool TryAddItemToSlot(int slot, ItemStack itemStack)
    {
        if (!this.SlotIsEmpty(slot))
        {
            if (!this.SlotHasCorrectItem(slot, itemStack))
            {
                return false;
            }

            ItemStack newStack = new ItemStack(itemStack.itemValue, items[slot].count + itemStack.count);

            this.items[slot] = newStack.Clone();
            this.tileEntityChanged();
            return true;
        }

        this.items[slot] = itemStack.Clone();
        this.tileEntityChanged();
        return true;
    }
    

    /**
     * Returns whether there is room in slots for this item stack. Pasing in a list of free spaces by reference means multiple stacks can be considered.
     */

    private bool RoomInSlotsFor(ItemStack itemStack, ref List<int> assignedSlots, out List<Tuple<int, ItemStack>> whereToAdd)
    {
        whereToAdd                                  = new List<Tuple<int, ItemStack>>();
        List<Tuple<int, ItemStack>> checkPositions  = new List<Tuple<int, ItemStack>>();
        bool matchingComplete                       = false;

        List<int> freeSpaceSlots;
        List<int> filledSpaceSlots;

        this.GetFreeAndFilledSlots(out freeSpaceSlots, out filledSpaceSlots);

        // Check the filled slots to see if existsing stacks can be added to first.
        if (filledSpaceSlots.Count > 0)
        {
            foreach (int slot in filledSpaceSlots)
            {
                int spacesLeft;
                int spacesNeeded;
                bool matchingStack;

                if (this.FilledSlotHasRoomFor(slot, itemStack, out spacesLeft, out spacesNeeded, out matchingStack))
                {
                    checkPositions.Add(new Tuple<int, ItemStack>(slot, itemStack));
                    matchingComplete = true;
                    break;
                }

                if (!matchingStack)
                {
                    continue;
                }

                ItemStack stackToFillThisSlot = new ItemStack(itemStack.itemValue, spacesLeft);
                checkPositions.Add(new Tuple<int, ItemStack>(slot, stackToFillThisSlot));
                itemStack.count -= spacesLeft; // Subsequent slots will have less to fill.
            }

            if (matchingComplete)
            {
                whereToAdd.AddRange(checkPositions);
                return true;
            }
        }
        
        // If there are no empty slots left, we have failed. No room.
        if (freeSpaceSlots.Count == 0)
        {
            return false;
        }


        // If we still have no match and there are empty slots, check if we can add it to free spaces.
        List<int> freePositionsToFill = new List<int>();
        foreach (int slot in freeSpaceSlots)
        {
            
            // This will prevent multiple calls to this method assigning same slot to an item stack.
            if (assignedSlots.Contains(slot))
            {
                continue;
            }

            freePositionsToFill.Add(slot);

            int spacesLeft;
            int spacesNeeded;

            if (this.EmptySlotHasRoomFor(slot, itemStack, out spacesLeft, out spacesNeeded))
            {
                checkPositions.Add(new Tuple<int, ItemStack>(slot, itemStack));
                matchingComplete = true;
                break;
            }

            ItemStack stackToFillThisSlot = new ItemStack(itemStack.itemValue, spacesLeft);
            checkPositions.Add(new Tuple<int, ItemStack>(slot, stackToFillThisSlot));
            itemStack.count -= spacesLeft; // Subsequent slots will have less to fill.
        }

        if (matchingComplete)
        {
            whereToAdd.AddRange(checkPositions);
            assignedSlots.AddRange(freePositionsToFill);
            return true;
        }

        return false;
    }
    

    /**
     * Outputs data on free and filled slots.
     * Outputs how many free and how many filled.
     * Gives a list of free slot indexes and filled slot indexes.
     */

    private void GetFreeAndFilledSlots(out List<int> freeSpaceSlots, out List<int> filledSpaceSlots)
    {
        freeSpaceSlots      = new List<int>();
        filledSpaceSlots    = new List<int>();
        for (int slot = 0; slot < this.items.Length; slot += 1)
        {
            if (this.SlotIsEmpty(slot) | this.items[slot] == null)
            {
                freeSpaceSlots.Add(slot);
            }
            else
            {
                filledSpaceSlots.Add(slot);
            }
        }
    }


    /**
     * Returns all itemstacks in filled slots
     */

    private ItemStack[] GetItemStacksFromFilledSlots()
    {
        ItemStack[] itemStacks = new ItemStack[this.items.Length];

        for (int slot = 0; slot < this.items.Length; slot += 1)
        {
            if (items[slot] != null && !items[slot].IsEmpty())
            {
                itemStacks[slot] = items[slot];
            }
        }
        return itemStacks;
    }


    /**
     * Returns true if slot is not filled with anything.
     */

    private bool SlotIsEmpty(int slot)
    {
        return this.items[slot].IsEmpty();
    }



    /**
     * Returns whether two item stacks contain the same item id
     */

    private bool StacksHaveSameItem(ItemStack stack1, ItemStack stack2)
    {
        return (stack1.itemValue.type == stack2.itemValue.type);
    }


    /**
     * Reads data into the tile entity when loaded.
     */

    public override void read(BinaryReader _br, StreamModeRead _eStreamMode)
    {
        base.read(_br, _eStreamMode);
        string collectionString = _br.ReadString();
        this.collection = TransformationCollection.Read(collectionString, useHash);
        string tQueueString = _br.ReadString();
        this.tQueue = tQueue.Read(tQueueString, useHash);
        this.requiresPower = _br.ReadBoolean();
        this.powerSources = this.ReadStringToList(_br.ReadString());
        this.requiresHeat = _br.ReadBoolean();
        this.heatSources = this.ReadStringToList(_br.ReadString());
    }


    /**
     * Saves tile entity data to the strem.
     */

    public override void write(BinaryWriter _bw, StreamModeWrite _eStreamMode)
    {
        base.write(_bw, _eStreamMode);
        _bw.Write(this.collection.Write(useHash));
        _bw.Write(this.tQueue.Write(useHash));
        _bw.Write(this.requiresPower);
        _bw.Write(this.WriteListToString(this.powerSources));
        _bw.Write(this.requiresHeat);
        _bw.Write(this.WriteListToString(this.heatSources));
        
    }


    /**
     * Writes a list to a comma separated string.
     */

    private string WriteListToString(List<string> _list)
    {
        switch (_list.Count)
        {
            case 0:
                return "";
            case 1:
                return _list[0];
            default:
                return String.Join(",", _list);
        }
    }


    /**
     * Reads a comma separated string into a list.
     */
    
    private List<string>ReadStringToList(string _s)
    {
        if (!_s.Contains(","))
        {
            return new List<string>() { _s };
        }

        string[] strings = _s.Split(',');
        List<string> list = new List<string>();
        foreach (string str in strings)
        {
            list.Add(str);
        }
        return list;
    }


    /**
     * What happens when the block is upgraded or downgraded.
     */

    public void UpgradeDowngradeFrom(TileEntityBlockTransformer _other)
    {
        base.UpgradeDowngradeFrom(_other);
        this.OnDestroy();
        if (_other is TileEntityBlockTransformer)
        {
            TileEntityBlockTransformer tileEntityBlockTransformer = _other as TileEntityBlockTransformer;
            this.bTouched = tileEntityBlockTransformer.bTouched;
            this.worldTimeTouched = tileEntityBlockTransformer.worldTimeTouched;
            this.bPlayerBackpack = tileEntityBlockTransformer.bPlayerBackpack;
            this.bPlayerStorage = tileEntityBlockTransformer.bPlayerStorage;
            this.items = ItemStack.Clone(tileEntityBlockTransformer.itemsArr, 0, this.containerSize.x * this.containerSize.y);
            if (this.items.Length != this.containerSize.x * this.containerSize.y)
            {
                Log.Error("Error upgrading.");
            }
        }
    }
    

    /**
     * What happens when the tile entities change state.
     */

    private void tileEntityChanged()
    {
        for (int i = 0; i < this.listeners.Count; i++)
        {
            this.listeners[i].OnTileEntityChanged(this, 0);
        }
    }


    /**
     * Returns the tile entity enum type for comparison.
     */

    public override TileEntityType GetTileEntityType()
    {
        return TileEntityType.BlockTransformer;
    }


    /**
     * Sets the collection variable to hold details of what items transform into what other ones.
     */

    public void SetTransformationCollection(TransformationCollection collection)
    {
        this.collection = collection;
    }


    /**
     * Sets required power.
     */

    public void SetRequirePower(bool requirePower, List<string> powerSourceBlocks)
    {
        this.requiresPower = requirePower;
        this.powerSources = powerSourceBlocks;
    }


    /**
     * Sets required heat.
     */
    
    public void SetRequireHeat(bool requireHeat, List<string> heatSourceBlocks)
    {
        this.requiresHeat = requireHeat;
        this.heatSources = heatSourceBlocks;
    }

    private bool useHash;
    private bool bDisableModifiedCheck;
    private bool requiresPower;
    private bool requiresHeat;
    private List<string> powerSources;
    private List<string> heatSources;
    private Vector2i containerSize;
    private ItemStack[] itemsArr;
    private List<Entity> entList;
    private TransformationCollection collection;
    private TransformationQueue tQueue;
}
