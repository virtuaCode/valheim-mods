using BepInEx;
using EquipWheel;

namespace EquipWheelTwo
{
    [BepInPlugin("virtuacode.valheim.equipwheeltwo", "Equip Wheel Mod (" + nameof(EquipWheelTwo) + ")", "0.0.1")]
    public class EquipWheel : BaseEquipWheel<EquipWheel> {}
}
