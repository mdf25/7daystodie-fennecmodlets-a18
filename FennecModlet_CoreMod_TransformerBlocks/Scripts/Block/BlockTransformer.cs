using System.Collections.Generic;
using GUI_2;

public class BlockTransformer : BlockLoot
{
	/**
	 *	Called when the block is loaded. This block has a tile entity attached, so need to set this to true.
	 */

    public BlockTransformer()
    {
        this.HasTileEntity = true;
    }


    /**
     * After initial block values loaded, this will load the extra properties for us.
     */

    public override void LateInit()
    {
        base.LateInit();
        this.transformationPropertyParser = new TransformationPropertyParser(this);
        this.transformationPropertyParser.ParseDynamicProperties();
        this.collection     = this.transformationPropertyParser.collection;
        this.requirePower   = this.transformationPropertyParser.requirePower;
        this.powerSources   = this.transformationPropertyParser.powerSources;
        this.requireHeat    = this.transformationPropertyParser.requireHeat;
        this.heatSources    = this.transformationPropertyParser.heatSources;
    }

    
    /**
     * This happens when the block is placed in the world.
     */

    public override void PlaceBlock(WorldBase _world, BlockPlacement.Result _result, EntityAlive _ea)
    {
        Block block = Block.list[_result.blockValue.type];
        if (block.shape.IsTerrain())
        {
            _world.SetBlockRPC(_result.clrIdx, _result.blockPos, _result.blockValue, this.Density);
        }
        else if (!block.IsTerrainDecoration)
        {
            _world.SetBlockRPC(_result.clrIdx, _result.blockPos, _result.blockValue, MarchingCubes.DensityAir);
        }
        else
        {
            _world.SetBlockRPC(_result.clrIdx, _result.blockPos, _result.blockValue);
        }

        TileEntityBlockTransformer tileEntityBlockTransformer = _world.GetTileEntity(_result.clrIdx, _result.blockPos) as TileEntityBlockTransformer;
        if (tileEntityBlockTransformer == null)
        {
            Log.Warning("Failed to create tile entity");
            return;
        }
        if (_ea != null && _ea.entityType == EntityType.Player)
        {
            tileEntityBlockTransformer.bPlayerStorage = true;
            tileEntityBlockTransformer.worldTimeTouched = _world.GetWorldTime();
            tileEntityBlockTransformer.SetEmpty();
        }
        Log.Out("Created successfullly.");
    }


    /**
     * This is the activation text that displays when the player looks at this block.
     */

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        TileEntityBlockTransformer tileEntityBlockTransformer = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityBlockTransformer;
        if (tileEntityBlockTransformer == null)
        {
            return string.Empty;
        }
        string arg = Localization.Get(Block.list[_blockValue.type].GetBlockName(), "");
        PlayerActionsLocal playerInput = ((EntityPlayerLocal)_entityFocusing).playerInput;
        string keybindString = UIUtils.GetKeybindString(playerInput.Activate, playerInput.PermanentActions.Activate);
        if (!tileEntityBlockTransformer.bTouched)
        {
            return string.Format(Localization.Get("lootTooltipNew", ""), keybindString, arg);
        }
        if (tileEntityBlockTransformer.IsEmpty())
        {
            return string.Format(Localization.Get("lootTooltipEmpty", ""), keybindString, arg);
        }
        return string.Format(Localization.Get("lootTooltipTouched", ""), keybindString, arg);
    }

    
    /**
     * This is called when block is removed. We want to destroy existing tile entity too.
     */

    public override void OnBlockRemoved(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        if (!_blockValue.ischild)
        {
            this.shape.OnBlockRemoved(_world, _chunk, _blockPos, _blockValue);
            if (this.isMultiBlock)
            {
                this.multiBlockPos.RemoveChilds(_world, _chunk.ClrIdx, _blockPos, _blockValue);
                return;
            }
        }
        else if (this.isMultiBlock)
        {
            this.multiBlockPos.RemoveParentBlock(_world, _chunk.ClrIdx, _blockPos, _blockValue);
        }
        
        TileEntityBlockTransformer tileEntityBlockTransformer = _world.GetTileEntity(_chunk.ClrIdx, _blockPos) as TileEntityBlockTransformer;
        if (tileEntityBlockTransformer != null)
        {
            tileEntityBlockTransformer.OnDestroy();
        }
        this.removeTileEntity(_world, _chunk, _blockPos, _blockValue);
    }


    /**
     * Helper method to add and set up a tile entity for the block.
     */

    protected override void addTileEntity(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        TileEntityBlockTransformer tileEntityBlockTransformer = new TileEntityBlockTransformer(_chunk);
        tileEntityBlockTransformer.localChunkPos = World.toBlock(_blockPos);
        tileEntityBlockTransformer.lootListIndex = (int)((ushort)this.lootList);
        tileEntityBlockTransformer.SetContainerSize(LootContainer.lootList[this.lootList].size, true);
        tileEntityBlockTransformer.SetTransformationCollection(this.collection);
        tileEntityBlockTransformer.SetRequirePower(this.requirePower, this.powerSources);
        tileEntityBlockTransformer.SetRequireHeat(this.requireHeat, this.heatSources);
        _chunk.AddTileEntity(tileEntityBlockTransformer);
    }

    
    /**
     * Helper method to remove a tile entity.
     */

    protected override void removeTileEntity(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        _chunk.RemoveTileEntityAt<TileEntityBlockTransformer>((World)world, World.toBlock(_blockPos));
    }

    
    /**
     * Called when a block is destroyed by an entity. It cam be useful for separating things out if needed depending on what
     * entity destroyed it.
     */

    public override bool OnBlockDestroyedBy(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _entityId, bool _bUseHarvestTool)
    {
        TileEntityBlockTransformer tileEntityBlockTransformer = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityBlockTransformer;
        if (tileEntityBlockTransformer != null)
        {
            tileEntityBlockTransformer.OnDestroy();
        }
        LocalPlayerUI uiforPlayer = LocalPlayerUI.GetUIForPlayer(GameManager.Instance.World.GetEntity(_entityId) as EntityPlayerLocal);
        if (null != uiforPlayer && uiforPlayer.windowManager.IsWindowOpen("looting") && ((XUiC_LootWindow)uiforPlayer.xui.GetWindow("windowLooting").Controller).GetLootBlockPos() == _blockPos)
        {
            uiforPlayer.windowManager.Close("looting");
        }
        if (tileEntityBlockTransformer != null)
        {
            _world.GetGameManager().DropContentOfLootContainerServer(_blockValue, _blockPos, tileEntityBlockTransformer.entityId);
        }
        return true;
    }


    /**
     * This is called when the player presses the activation key (usually E) on the block.
     */

    public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        TileEntityBlockTransformer tileEntityBlockTransformer = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityBlockTransformer;
        if (tileEntityBlockTransformer == null)
        {
            return false;
        }
        _player.AimingGun = false;
        Vector3i blockPos = tileEntityBlockTransformer.ToWorldPos();
        tileEntityBlockTransformer.bWasTouched = tileEntityBlockTransformer.bTouched;
        _world.GetGameManager().TELockServer(_cIdx, blockPos, tileEntityBlockTransformer.entityId, _player.entityId);
        return true;
    }


    /**
     * Returns the commands to activate the block looking at.
     */

    private BlockActivationCommand[] cmds = new BlockActivationCommand[]
    {
        new BlockActivationCommand("Search", "search", true)
    };


    // Transformation properties to pass on.
    private TransformationPropertyParser transformationPropertyParser;
    private TransformationCollection collection;
    private bool requirePower;
    private bool requireHeat;
    private List<string> powerSources;
    private List<string> heatSources;
}
