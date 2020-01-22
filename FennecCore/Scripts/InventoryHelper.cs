using System;
using System.Collections.Generic;
using UnityEngine;

/**
 * This class is for navigating inventories, checking items in slots, etc.
 * This can be used universally across all tile entity classes or those that need to check inventories for items in slots.
 */

public static class InventoryHelper
{
    /**
     * Checks that the slot contains the correct item stack.
     */

    public static bool SlotHasCorrectItem(ItemStack[] inventory, int slot, ItemStack itemStack)
    {
        return (StacksHaveSameItem(inventory[slot], itemStack));
    }


    /**
     * Remove a number {remove} of items from the item stack at slot {slot} if possible. Returns whether items were removed successfully or not.
     */

    public static bool RemoveItemsInSlot(ItemStack[] inventory, int slot, int remove)
    {
        if (!CanRemoveItemsInSlot(inventory, slot, remove))
        {
            return false;
        }

        ItemStack newStack = new ItemStack(inventory[slot].itemValue, inventory[slot].count - remove);
        inventory[slot] = newStack.Clone();

        if (inventory[slot].count <= 0)
        {
            inventory[slot].Clear();
        }
        return true;
    }


    /**
     * Checks that we can remove the item in slot {slot}
     */

    public static bool CanRemoveItemsInSlot(ItemStack[] inventory, int slot, int remove)
    {
        // If slot is not defined, return false
        if (slot > inventory.Length - 1)
        {
            return false;
        }

        // If we have no itemstack defined, terminate to avoid nullref.
        if (inventory[slot] == null)
        {
            Log.Warning("Item slot " + slot.ToString() + " is null");
            return false;
        }

        // If the itemstack is empty, terminate.
        if (inventory[slot].IsEmpty())
        {
            Log.Warning("Item slot " + slot.ToString() + " is empty.");
            return false;
        }

        // If the number to remove is not 
        if (remove > inventory[slot].count)
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

    public static bool FilledSlotHasRoomFor(ItemStack[] inventory, int slot, ItemStack itemStack, out int spacesLeft, out int spacesNeeded, out bool matchingStack)
    {
        if (!SlotHasCorrectItem(inventory, slot, itemStack))
        {
            spacesLeft = spacesNeeded = 0;
            return matchingStack = false;
        }

        matchingStack = true;

        int stackNumber = TotalStackSizeFor(itemStack);
        int numAlready = GetNumItemsInSlot(inventory, slot);
        int numToAdd = itemStack.count;

        spacesLeft = Mathf.Max(stackNumber - numAlready, 0);
        spacesNeeded = Mathf.Max(numAlready + numToAdd - stackNumber, 0);

        return (spacesLeft >= 0 && spacesNeeded == 0);
    }


    /**
     * Returns whether an empty slot has room to stack something completely.
     * Outputs the number of spaces in the slot
     * Outputs the number of spaces needed, if the stack can't completely fill the slot. 
     */

    public static bool EmptySlotHasRoomFor(ItemStack[] inventory, int slot, ItemStack itemStack, out int spacesLeft, out int spacesNeeded)
    {
        if (!SlotIsEmpty(inventory, slot))
        {
            spacesLeft = spacesNeeded = 0;
            return false;
        }

        int stackNumber = TotalStackSizeFor(itemStack);
        int numToAdd = itemStack.count;

        spacesLeft = Mathf.Max(stackNumber, 0);
        spacesNeeded = Mathf.Max(numToAdd - stackNumber, 0);

        return (spacesLeft >= 0 && spacesNeeded == 0);
    }


    /**
     * Gets how many items are in a slot
     */

    public static int GetNumItemsInSlot(ItemStack[] inventory, int slot)
    {
        if (inventory[slot].itemValue.type == 0)
        {
            return 0;
        }
        return inventory[slot].count;
    }


    /**
     * Gets the stack number for an item stack.
     */

    public static int TotalStackSizeFor(ItemStack itemStack)
    {
        return ItemClass.GetForId(itemStack.itemValue.type).Stacknumber.Value;
    }


    /**
     * Tries to add an item to a slot.
     */

    public static bool TryAddItemToSlot(ItemStack[] inventory, int slot, ItemStack itemStack)
    {
        if (!SlotIsEmpty(inventory, slot))
        {
            if (!SlotHasCorrectItem(inventory, slot, itemStack))
            {
                return false;
            }

            ItemStack newStack = new ItemStack(itemStack.itemValue, inventory[slot].count + itemStack.count);

            inventory[slot] = newStack.Clone();
            return true;
        }

        inventory[slot] = itemStack.Clone();
        return true;
    }


    /**
     * Returns whether there is room in slots for this item stack. 
     * Passing in a list of free spaces by reference means multiple stacks can be considered.
     */

    public static bool RoomInSlotsFor(ItemStack[] inventory, ItemStack itemStack, ref List<int> assignedSlots, out List<Tuple<int, ItemStack>> whereToAdd)
    {
        whereToAdd = new List<Tuple<int, ItemStack>>();
        List<Tuple<int, ItemStack>> checkPositions = new List<Tuple<int, ItemStack>>();
        bool matchingComplete = false;

        List<int> freeSpaceSlots;
        List<int> filledSpaceSlots;

        GetFreeAndFilledSlots(inventory, out freeSpaceSlots, out filledSpaceSlots);

        // Check the filled slots to see if existsing stacks can be added to first.
        if (filledSpaceSlots.Count > 0)
        {
            foreach (int slot in filledSpaceSlots)
            {
                int spacesLeft;
                int spacesNeeded;
                bool matchingStack;

                if (FilledSlotHasRoomFor(inventory, slot, itemStack, out spacesLeft, out spacesNeeded, out matchingStack))
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

            if (EmptySlotHasRoomFor(inventory, slot, itemStack, out spacesLeft, out spacesNeeded))
            {
                checkPositions.Add(new Tuple<int, ItemStack>(slot, itemStack));
                matchingComplete = true;
                break;
            }

            ItemStack stackToFillThisSlot = new ItemStack(itemStack.itemValue, spacesLeft);
            checkPositions.Add(new Tuple<int, ItemStack>(slot, stackToFillThisSlot));
            itemStack.count -= spacesLeft;
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
     * Outputs data on free and filled slots in an inventory.
     * Gives a list of free slot indexes and filled slot indexes.
     */

    public static void GetFreeAndFilledSlots(ItemStack[] inventory, out List<int> freeSpaceSlots, out List<int> filledSpaceSlots)
    {
        freeSpaceSlots = new List<int>();
        filledSpaceSlots = new List<int>();
        for (int slot = 0; slot < inventory.Length; slot += 1)
        {
            if (SlotIsEmpty(inventory, slot) | inventory[slot] == null)
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
     * Returns all non empty itemstacks in an inventory.
     */

    public static ItemStack[] GetItemStacksFromFilledSlots(ItemStack[] inventory)
    {
        ItemStack[] itemStacks = new ItemStack[inventory.Length];

        for (int slot = 0; slot < inventory.Length; slot += 1)
        {
            if (inventory[slot] != null && !inventory[slot].IsEmpty())
            {
                itemStacks[slot] = inventory[slot];
            }
        }
        return itemStacks;
    }


    /**
     * Returns true if an inventory slot is not filled with anything.
     */

    public static bool SlotIsEmpty(ItemStack[] inventory, int slot)
    {
        return inventory[slot].IsEmpty();
    }



    /**
     * Returns whether two itemstacks are for the same item.
     */

    public static bool StacksHaveSameItem(ItemStack stack1, ItemStack stack2)
    {
        return (stack1.itemValue.type == stack2.itemValue.type);
    }
}