using NSubstitute;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class inventory  
{
    [Test]
    public void only_allows_one_chest_to_be_equipped_at_a_time()
    {
        //Arrange
        ICharacter character = Substitute.For<ICharacter>();
        Inventory inventory = new Inventory(character);
        Item chestOne= new Item() { EquipSlot = EquipSlots.Chest };
        Item chestTwo= new Item() { EquipSlot = EquipSlots.Chest };


        //Act
        inventory.EquipItem(chestOne);
        inventory.EquipItem(chestTwo);


        //Assert
        Item equippedItem = inventory.GetItem(equipSlot: EquipSlots.Chest);
        Assert.AreEqual(expected: chestTwo, actual: equippedItem);
    }


    [Test]
    public void tells_character_when_an_item_is_eguipped_succesfully()
    {
        //Arrange
        ICharacter character = Substitute.For<ICharacter>();
        Inventory inventory = new Inventory(character);
        Item chestOne = new Item() { EquipSlot = EquipSlots.Chest };
        Item chestTwo = new Item() { EquipSlot = EquipSlots.Chest };


        //Act
        inventory.EquipItem(chestOne);

        //Assert
        character.Received().OnItemEquipped(chestOne);
    }
}
