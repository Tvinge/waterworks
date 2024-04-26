using NUnit.Framework;
using NSubstitute;
public class character_with_inventory
{
    [Test]
    public void with_90_armor_takies_10_percent_damage()
    {
        //Arrange
        ICharacter character = Substitute.For<ICharacter>();
        Inventory inventory = new Inventory(character);
        Item pants = new Item() { EquipSlot = EquipSlots.Legs, Armor = 40 };
        Item shield = new Item() { EquipSlot = EquipSlots.RightHand, Armor = 50 };

        inventory.EquipItem(pants);
        inventory.EquipItem(shield);

        character.Inventory.Returns(inventory);

        //Act
        int calculatedDamage = DamageCalculator.CalculateDamage(amount: 1000, character);

        //Assert
        Assert.AreEqual(100, calculatedDamage);
    }
}
