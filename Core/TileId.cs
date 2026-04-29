public enum TileId : byte
{
    Empty        = 0,   // '\0' / ' '
    Plains       = 1,   // 'P'
    Forest       = 2,   // 'F'
    Mountain     = 3,   // 'M'
    MountainDeep = 4,   // 'm' — surrounded by 8 mountain neighbors
    Snow         = 5,   // 'S' — mountain tops
    River        = 6,   // 'R'
    RiverShallow = 7,   // 'r'
    Lake         = 8,   // 'L'
    LakeShallow  = 9,   // 'l'
    Ocean        = 10,  // 'O'
    OceanShallow = 11,  // 'o'
    Beach        = 12,  // 'B'
    BeachDark    = 13,  // 'b'
    Stream       = 14,  // 's' — shallow stream/water (NOT snow shallow)
    Border       = 15,  // '@'
}
