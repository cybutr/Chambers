using System.Collections.Generic;
using Internal;

public static class TileRegistry
{
    private static readonly TileDefinition[] _defs = new TileDefinition[16];
    private static readonly Dictionary<char, TileId> _byChar = new();

    static TileRegistry()
    {
        Register(new TileDefinition {
            Id=TileId.Empty, Name="Empty", LegacyChar=' ',
            BaseColor=ColorSpectrum.BLUE, MovementCost=1.0f });

        Register(new TileDefinition {
            Id=TileId.Plains, Name="Plains", LegacyChar='P',
            BaseColor=ColorSpectrum.GREEN, MovementCost=0.8f,
            IsLand=true,
            CreatureCategories=new HashSet<string>{"land"} });

        Register(new TileDefinition {
            Id=TileId.Forest, Name="Forest", LegacyChar='F',
            BaseColor=ColorSpectrum.DARK_GREEN, MovementCost=0.2f,
            IsLand=true,
            CreatureCategories=new HashSet<string>{"land","forest"} });

        Register(new TileDefinition {
            Id=TileId.Mountain, Name="Mountain", LegacyChar='M',
            BaseColor=ColorSpectrum.DARK_GREY, MovementCost=10.0f,
            IsLand=true });

        Register(new TileDefinition {
            Id=TileId.MountainDeep, Name="Mountain (Deep)", LegacyChar='m',
            BaseColor=ColorSpectrum.GREY, MovementCost=15.0f,
            IsLand=true });

        Register(new TileDefinition {
            Id=TileId.Snow, Name="Snow", LegacyChar='S',
            BaseColor=ColorSpectrum.WHITE, MovementCost=20.0f,
            IsLand=true });

        Register(new TileDefinition {
            Id=TileId.River, Name="River", LegacyChar='R',
            BaseColor=ColorSpectrum.BLUE, MovementCost=4.0f,
            IsWater=true, DeepVariant=TileId.RiverShallow,
            CreatureCategories=new HashSet<string>{"water"} });

        Register(new TileDefinition {
            Id=TileId.RiverShallow, Name="River (Shallow)", LegacyChar='r',
            BaseColor=ColorSpectrum.DARKER_BLUE, MovementCost=3.0f,
            IsWater=true,
            CreatureCategories=new HashSet<string>{"water","shallow_water"} });

        Register(new TileDefinition {
            Id=TileId.Lake, Name="Lake", LegacyChar='L',
            BaseColor=ColorSpectrum.BLUE, MovementCost=4.0f,
            IsWater=true, DeepVariant=TileId.LakeShallow,
            CreatureCategories=new HashSet<string>{"water"} });

        Register(new TileDefinition {
            Id=TileId.LakeShallow, Name="Lake (Shallow)", LegacyChar='l',
            BaseColor=ColorSpectrum.DARKER_BLUE, MovementCost=3.0f,
            IsWater=true,
            CreatureCategories=new HashSet<string>{"water","shallow_water"} });

        Register(new TileDefinition {
            Id=TileId.Ocean, Name="Ocean", LegacyChar='O',
            BaseColor=ColorSpectrum.BLUE, MovementCost=5.0f,
            IsWater=true, DeepVariant=TileId.OceanShallow,
            CreatureCategories=new HashSet<string>{"water","deep_water"} });

        Register(new TileDefinition {
            Id=TileId.OceanShallow, Name="Ocean (Shallow)", LegacyChar='o',
            BaseColor=ColorSpectrum.DARKER_BLUE, MovementCost=6.0f,
            IsWater=true,
            CreatureCategories=new HashSet<string>{"water","shallow_water"} });

        Register(new TileDefinition {
            Id=TileId.Beach, Name="Beach", LegacyChar='B',
            BaseColor=ColorSpectrum.YELLOW, MovementCost=0.5f,
            IsLand=true,
            CreatureCategories=new HashSet<string>{"beach","land"} });

        Register(new TileDefinition {
            Id=TileId.BeachDark, Name="Beach (Dark)", LegacyChar='b',
            BaseColor=ColorSpectrum.DARK_YELLOW, MovementCost=0.7f,
            IsLand=true,
            CreatureCategories=new HashSet<string>{"beach","land"} });

        Register(new TileDefinition {
            Id=TileId.Stream, Name="Stream", LegacyChar='s',
            BaseColor=ColorSpectrum.BLUE, MovementCost=3.0f,
            IsWater=true, DeepVariant=TileId.RiverShallow,
            CreatureCategories=new HashSet<string>{"water","shallow_water"} });

        Register(new TileDefinition {
            Id=TileId.Border, Name="Border", LegacyChar='@',
            BaseColor=ColorSpectrum.SILVER, MovementCost=float.MaxValue });
    }

    private static void Register(TileDefinition def)
    {
        _defs[(int)def.Id] = def;
        _byChar[def.LegacyChar] = def.Id;
    }

    public static TileDefinition  Get(TileId id)   => _defs[(int)id];
    public static TileId          FromChar(char c)  => _byChar.TryGetValue(c, out var id) ? id : TileId.Empty;
    public static char            ToChar(TileId id) => _defs[(int)id].LegacyChar;
    public static bool            IsValid(TileId id)=> (int)id < _defs.Length && _defs[(int)id] != null;
}
