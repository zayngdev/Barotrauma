﻿using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Barotrauma
{
    partial class CharacterInventory : Inventory
    {        
        private static Sprite toggleArrow;
        private float arrowAlpha;

        //which slot is the toggle arrow drawn above
        private int toggleArrowSlotIndex;
        
        public Vector2[] SlotPositions;
        
        private Alignment alignment;
        public Alignment Alignment
        {
            get { return alignment; }
            set
            {
                if (alignment == value) return;
                alignment = value;
                SetSlotPositions(alignment);
            }
        }

        private bool hidden;
        public bool Hidden
        {
            get { return hidden; }
            set { hidden = value; }
        }
        
        partial void InitProjSpecific(XElement element)
        {
            if (toggleArrow == null)
            {
                toggleArrow = new Sprite("Content/UI/inventoryAtlas.png", new Rectangle(585, 973, 67, 23), null);
                toggleArrow.Origin = toggleArrow.size / 2;
            }

            toggleArrowSlotIndex = MathHelper.Clamp( element.GetAttributeInt("arrowslot", 0), 0, capacity - 1);

            hidden = true;

            SlotPositions = new Vector2[SlotTypes.Length];
            Alignment = Alignment.Center;
        }

        private bool UseItemOnSelf(GUIButton button, object obj)
        {
            if (!(obj is int)) return false;

            int slotIndex = (int)obj;

            return UseItemOnSelf(slotIndex);
        }


        protected override void PutItem(Item item, int i, Character user, bool removeItem = true, bool createNetworkEvent = true)
        {
            base.PutItem(item, i, user, removeItem, createNetworkEvent);
            CreateSlots();
        }

        public override void RemoveItem(Item item)
        {
            base.RemoveItem(item);
            CreateSlots();
        }

        protected override void CreateSlots()
        {
            if (slots == null) slots = new InventorySlot[capacity];
            
            for (int i = 0; i < capacity; i++)
            {
                InventorySlot prevSlot = slots[i];
                
                Sprite slotSprite = slotSpriteSmall;
                Rectangle slotRect = new Rectangle(
                    (int)(SlotPositions[i].X), 
                    (int)(SlotPositions[i].Y),
                    (int)(slotSprite.size.X * UIScale), (int)(slotSprite.size.Y * UIScale));

                if (Items[i] != null)
                {
                    ItemContainer itemContainer = Items[i].GetComponent<ItemContainer>();
                    if (itemContainer != null)
                    {
                        if (itemContainer.InventoryTopSprite != null) slotRect.Width = Math.Max(slotRect.Width, (int)(itemContainer.InventoryTopSprite.size.X * UIScale));
                        if (itemContainer.InventoryBottomSprite != null) slotRect.Width = Math.Max(slotRect.Width, (int)(itemContainer.InventoryBottomSprite.size.X * UIScale));
                    }
                }

                slots[i] = new InventorySlot(slotRect)
                {
                    Disabled = false,
                    SlotSprite = slotSprite,
                    Color = SlotTypes[i] == InvSlotType.Any ? Color.White * 0.2f : Color.White * 0.4f
                };
                if (prevSlot != null)
                {
                    slots[i].DrawOffset = prevSlot.DrawOffset;
                    slots[i].Color = prevSlot.Color;
                }

                if (selectedSlot?.ParentInventory == this && selectedSlot.SlotIndex == i)
                {
                    selectedSlot = new SlotReference(this, slots[i], i, selectedSlot.IsSubSlot, selectedSlot.Inventory);
                }
            }

            AssignQuickUseNumKeys();

            highlightedSubInventorySlots.Clear();

            screenResolution = new Point(GameMain.GraphicsWidth, GameMain.GraphicsHeight);
        }

        protected override bool HideSlot(int i)
        {
            if (slots[i].Disabled || (hideEmptySlot[i] && Items[i] == null)) return true;

            //no need to draw the right hand slot if the item is in both hands
            if (Items[i] != null && SlotTypes[i] == InvSlotType.RightHand && IsInLimbSlot(Items[i], InvSlotType.LeftHand))
            {
                return true;
            }

            //don't show the equip slot if the item is also in the default inventory
            if (SlotTypes[i] != InvSlotType.Any && Items[i] != null)
            {
                for (int j = 0; j < capacity; j++)
                {
                    if (SlotTypes[j] == InvSlotType.Any && Items[j] == Items[i]) return true;
                }
            }

            return false;
        }

        private void SetSlotPositions(Alignment alignment)
        {
            int spacing = 10;
            int x = GameMain.GraphicsWidth / 2;
            int x2 = x;

            if (alignment == Alignment.Center)
            {
                x = GameMain.GraphicsWidth - HUDLayoutSettings.Padding - (int)(SlotTypes.Count(s => s == InvSlotType.Any) * (slotSpriteVertical.size.X + spacing) * UIScale);
            }
            else if (alignment == Alignment.Right)
            {
                x = HUDLayoutSettings.InventoryAreaLower.Right - (int)(SlotTypes.Count(s => s == InvSlotType.Any) * (slotSpriteVertical.size.X + spacing) * UIScale);
                x2 = HUDLayoutSettings.InventoryAreaLower.Right;
                for (int i = 0; i<slots.Length; i++)
                {
                    if (SlotTypes[i] == InvSlotType.Any || HideSlot(i)) continue;
                    x2 -= (int)((slotSpriteVertical.size.X + spacing) * UIScale);
                }
            }
            else if (alignment == Alignment.Left)
            {
                x = HUDLayoutSettings.InventoryAreaLower.X;
                x2 = x;
            }

            int offsetFromBottom = (int)((slotSpriteSmall.size.Y + spacing * 2) * UIScale);

            for (int i = 0; i < SlotPositions.Length; i++)
            {
                if (alignment == Alignment.Center)
                {
                    switch (SlotTypes[i])
                    {
                        case InvSlotType.Headset:
                            SlotPositions[i] = new Vector2(
                                HUDLayoutSettings.InventoryAreaUpper.Right - (slotSpriteSmall.size.X + spacing) * UIScale, 
                                HUDLayoutSettings.InventoryAreaUpper.Y);
                            break;
                        case InvSlotType.Card:
                            SlotPositions[i] = new Vector2(
                                HUDLayoutSettings.InventoryAreaUpper.Right - (slotSpriteSmall.size.X + spacing) * 2 * UIScale, 
                                HUDLayoutSettings.InventoryAreaUpper.Y);
                            break;
                        case InvSlotType.InnerClothes:
                            SlotPositions[i] = new Vector2(
                                HUDLayoutSettings.InventoryAreaUpper.Right - (slotSpriteSmall.size.X + spacing) * 3 * UIScale, 
                                HUDLayoutSettings.InventoryAreaUpper.Y);
                            break;
                        case InvSlotType.Head:
                            SlotPositions[i] = new Vector2(
                                HUDLayoutSettings.InventoryAreaUpper.Right - (slotSpriteSmall.size.X + spacing) * 4 * UIScale, 
                                HUDLayoutSettings.InventoryAreaUpper.Y);
                            break;
                        case InvSlotType.OuterClothes:
                            SlotPositions[i] = new Vector2(GameMain.GraphicsWidth / 2 - 200 * UIScale, GameMain.GraphicsHeight - offsetFromBottom);
                            break;
                        case InvSlotType.LeftHand:
                            SlotPositions[i] = new Vector2(GameMain.GraphicsWidth / 2 - 130 * UIScale, GameMain.GraphicsHeight - offsetFromBottom);
                            break;
                        case InvSlotType.RightHand:
                            SlotPositions[i] = new Vector2(GameMain.GraphicsWidth / 2 - 60 * UIScale, GameMain.GraphicsHeight - offsetFromBottom);
                            break;
                        case InvSlotType.Any:
                            SlotPositions[i] = new Vector2(x, GameMain.GraphicsHeight - offsetFromBottom);
                            x += (int)((slotSpriteVertical.size.X + spacing) * UIScale);
                            break;
                    }
                }
                else if (alignment == Alignment.Left)
                {
                    if (HideSlot(i)) continue;
                    if (SlotTypes[i] == InvSlotType.Card || SlotTypes[i] == InvSlotType.Headset || SlotTypes[i] == InvSlotType.InnerClothes)
                    {
                        SlotPositions[i] = new Vector2(x2, GameMain.GraphicsHeight - offsetFromBottom * 2 - spacing * 2);
                        x2 += (int)((slots[i].Rect.Width + spacing * UIScale));
                    }
                    else
                    {
                        SlotPositions[i] = new Vector2(x, GameMain.GraphicsHeight - offsetFromBottom);
                        x += (int)((slots[i].Rect.Width + spacing * UIScale));
                    }
                }
                else if (alignment == Alignment.Right)
                {
                    if (HideSlot(i)) continue;
                    if (SlotTypes[i] == InvSlotType.Card || SlotTypes[i] == InvSlotType.Headset || SlotTypes[i] == InvSlotType.InnerClothes)
                    {
                        SlotPositions[i] = new Vector2(x2, GameMain.GraphicsHeight - offsetFromBottom * 2 - spacing * 2);
                        if (i < slots.Length - 1)
                        {
                            x2 += (int)((slots[i].Rect.Width + spacing * UIScale));
                        }
                    }
                    else
                    {
                        SlotPositions[i] = new Vector2(x, GameMain.GraphicsHeight - offsetFromBottom);
                        if (i < slots.Length - 1)
                        {
                            x += (int)((slots[i].Rect.Width + spacing * UIScale));
                        }
                    }
                }
            }

            CreateSlots();
        }

        public override void Update(float deltaTime, bool isSubInventory = false)
        {
            if (!AccessibleWhenAlive && !character.IsDead) return;

            base.Update(deltaTime);

            bool hoverOnInventory = GUI.MouseOn == null &&
                ((selectedSlot != null && selectedSlot.IsSubSlot) || (draggingItem != null && (draggingSlot == null || !draggingSlot.MouseOn())));
            if (CharacterHealth.OpenHealthWindow != null) hoverOnInventory = true;

            if (alignment == Alignment.Center)
            {
                Rectangle arrowRect = new Rectangle(
                    (int)(slots[toggleArrowSlotIndex].Rect.Center.X + slots[toggleArrowSlotIndex].DrawOffset.X - toggleArrow.size.X / 2),
                    (int)(slots[toggleArrowSlotIndex].Rect.Y + slots[toggleArrowSlotIndex].DrawOffset.Y - 50 - toggleArrow.size.Y / 2), 
                    (int)toggleArrow.size.X, (int)toggleArrow.size.Y);
                arrowRect.Inflate(30, 0);

                if (arrowRect.Contains(PlayerInput.MousePosition))
                {
                    arrowAlpha = Math.Min(arrowAlpha + deltaTime * 10.0f, 1.0f);
                    if (PlayerInput.LeftButtonClicked())
                    {
                        hidden = !hidden;
                        HideTimer = 0.0f;

                        foreach (var highlightedSubInventorySlot in highlightedSubInventorySlots)
                        {
                            highlightedSubInventorySlot.Inventory.HideTimer = 0.0f;
                        }
                        return;
                    }
                }
                else
                {
                    arrowAlpha = Math.Max(arrowAlpha - deltaTime * 10.0f, 0.5f);
                }

                if (GUI.MouseOn == null &&
                    (slots[toggleArrowSlotIndex].DrawOffset.Y < 10.0f && PlayerInput.MousePosition.Y > arrowRect.Bottom ||
                    slots[toggleArrowSlotIndex].DrawOffset.Y > 10.0f && PlayerInput.MousePosition.Y > slots[toggleArrowSlotIndex].EquipButtonRect.Bottom) &&
                    slots.Any(s => PlayerInput.MousePosition.X > s.InteractRect.X - 10 && PlayerInput.MousePosition.X < s.InteractRect.Right + 10))
                {
                    hoverOnInventory = true;
                }
            }

            if (hoverOnInventory) HideTimer = 0.5f;
            if (HideTimer > 0.0f) HideTimer -= deltaTime;

            for (int i = 0; i < capacity; i++)
            {
                /*if (SlotTypes[i] == InvSlotType.Any || SlotTypes[i] == InvSlotType.OuterClothes ||
                    SlotTypes[i] == InvSlotType.LeftHand || SlotTypes[i] == InvSlotType.RightHand)
                {
                    if (hidden && !hoverOnInventory && alignment == Alignment.Center && HideTimer <= 0.0f)
                    {
                        slots[i].DrawOffset =
                            new Vector2(slots[i].DrawOffset.X, MathHelper.Lerp(slots[i].DrawOffset.Y, slots[i].Rect.Size.Y / 2 + slotSpriteRound.size.Y * UIScale, 10.0f * deltaTime));
                    }
                    else
                    {
                        slots[i].DrawOffset =
                            new Vector2(slots[i].DrawOffset.X, MathHelper.Lerp(slots[i].DrawOffset.Y, 0, 10.0f * deltaTime));
                    }
                }*/
                if (Items[i] != null && Character.Controlled?.Inventory == this && 
                    slots[i].QuickUseKey != Keys.None && PlayerInput.KeyHit(slots[i].QuickUseKey))
                {
                    QuickUseItem(Items[i], true, false);
                }
            }
            
            List<SlotReference> hideSubInventories = new List<SlotReference>();
            foreach (var highlightedSubInventorySlot in highlightedSubInventorySlots)
            {
                if (highlightedSubInventorySlot.ParentInventory == this)
                {
                    UpdateSubInventory(deltaTime, highlightedSubInventorySlot.SlotIndex);
                }
                
                Rectangle hoverArea = highlightedSubInventorySlot.Slot.Rect;
                hoverArea.Location += highlightedSubInventorySlot.Slot.DrawOffset.ToPoint();
                hoverArea = Rectangle.Union(hoverArea, highlightedSubInventorySlot.Slot.EquipButtonRect);
                if (highlightedSubInventorySlot.Inventory?.slots != null)
                {
                    foreach (InventorySlot slot in highlightedSubInventorySlot.Inventory.slots)
                    {
                        Rectangle subSlotRect = slot.InteractRect;
                        subSlotRect.Location += slot.DrawOffset.ToPoint();
                        hoverArea = Rectangle.Union(hoverArea, subSlotRect);
                    }
                }
                hoverArea.Inflate(10, 10);

                if (highlightedSubInventorySlot.Inventory?.slots == null || (!hoverArea.Contains(PlayerInput.MousePosition)))
                {
                    hideSubInventories.Add(highlightedSubInventorySlot);
                }
                else
                {
                    highlightedSubInventorySlot.Inventory.HideTimer = 1.0f;
                }
            }

            if (doubleClickedItem != null)
            {
                QuickUseItem(doubleClickedItem, true, true);
            }

            //make subinventories with one slot always visible
           /* for (int i = 0; i < capacity; i++)
            {
                Inventory subInventory = GetSubInventory(i);
                if (subInventory != null && subInventory.Capacity == 1)
                {
                    UpdateSubInventory(deltaTime, i);
                }
            }*/

            //activate the subinventory of the currently selected slot
            if (selectedSlot?.ParentInventory == this)
            {
                var subInventory = GetSubInventory(selectedSlot.SlotIndex);
                if (subInventory != null)
                {
                    selectedSlot.Inventory = subInventory;
                    if (!highlightedSubInventorySlots.Any(s => s.Inventory == subInventory))
                    {
                        highlightedSubInventorySlots.Add(selectedSlot);
                        UpdateSubInventory(deltaTime, selectedSlot.SlotIndex);
                    }
                }
            }
                       
            foreach (var subInventorySlot in hideSubInventories)
            {
                if (subInventorySlot.Inventory == null) continue;
                subInventorySlot.Inventory.HideTimer -= deltaTime;
                if (subInventorySlot.Inventory.HideTimer <= 0.0f)
                {
                    highlightedSubInventorySlots.Remove(subInventorySlot);
                }
            }

            if (character == Character.Controlled)
            {
                for (int i = 0; i < capacity; i++)
                {
                    /*if ((selectedSlot == null || selectedSlot.SlotIndex != i) &&
                        Items[i] != null && Items[i].CanUseOnSelf && character.HasSelectedItem(Items[i]))
                    {
                        //-3 because selected items are in slots 3 and 4 (hands)
                        useOnSelfButton[i - 3].Update(deltaTime);
                    }*/

                    if (Items[i] != null && Owner == Character.Controlled && Items[i].AllowedSlots.Any(a => a != InvSlotType.Any))
                    {
                        slots[i].EquipButtonState = slots[i].EquipButtonRect.Contains(PlayerInput.MousePosition) ? 
                            GUIComponent.ComponentState.Hover : GUIComponent.ComponentState.None;

                        if (slots[i].EquipButtonState == GUIComponent.ComponentState.Hover)
                        {
                            if (PlayerInput.LeftButtonDown()) slots[i].EquipButtonState = GUIComponent.ComponentState.Pressed;
                            if (PlayerInput.LeftButtonClicked())
                            {
                                QuickUseItem(Items[i], true, false);
                            }
                        }
                    }
                }
            }

            //cancel dragging if too far away from the container of the dragged item
            if (draggingItem != null)
            {
                var rootContainer = draggingItem.GetRootContainer();
                var rootInventory = draggingItem.ParentInventory;

                if (rootContainer != null)
                {
                    rootInventory = rootContainer.ParentInventory != null ?
                        rootContainer.ParentInventory : rootContainer.GetComponent<ItemContainer>().Inventory;
                }

                if (rootInventory != null &&
                    rootInventory.Owner != Character.Controlled &&
                    rootInventory.Owner != Character.Controlled.SelectedConstruction &&
                    rootInventory.Owner != Character.Controlled.SelectedCharacter)
                {
                    draggingItem = null;
                }
            }

            doubleClickedItem = null;
        }

        private void AssignQuickUseNumKeys()
        {
            int num = 1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (HideSlot(i))
                {
                    slots[i].QuickUseKey = Keys.None;
                    continue;
                }

                if (SlotTypes[i] == InvSlotType.Any)
                {
                    slots[i].QuickUseKey = Keys.D0 + num % 10;
                    num++;
                }
            }

            /*for (int i = 0; i < slots.Length; i++)
            {
                if (HideSlot(i)) continue;
                
                //assign non-limb specific slots first to make them start from 1
                if (SlotTypes[i] != InvSlotType.Any)
                {
                    slots[i].QuickUseKey = Keys.D0 + num;
                    num++;
                }
            }*/
        }
        
        private void QuickUseItem(Item item, bool allowEquip, bool allowInventorySwap)
        {
            bool wasPut = false;
            if (item.ParentInventory != this)
            {
                //in another inventory -> attempt to place in the character's inventory
                if (allowInventorySwap) wasPut = TryPutItem(item, Character.Controlled, item.AllowedSlots, true);
            }
            else
            {
                var selectedContainer = character.SelectedConstruction?.GetComponent<ItemContainer>();
                if (selectedContainer != null && selectedContainer.Inventory != null && allowInventorySwap)
                {
                    //player has selected the inventory of another item -> attempt to move the item there
                    wasPut = selectedContainer.Inventory.TryPutItem(item, Character.Controlled, item.AllowedSlots, true);
                }
                else if (character.SelectedCharacter != null && character.SelectedCharacter.Inventory != null && allowInventorySwap)
                {
                    //player has selected the inventory of another character -> attempt to move the item there
                    wasPut = character.SelectedCharacter.Inventory.TryPutItem(item, Character.Controlled, item.AllowedSlots, true);
                }
                else if (character.SelectedBy != null && Character.Controlled == character.SelectedBy && 
                    character.SelectedBy.Inventory != null && allowInventorySwap)
                {
                    //item is in the inventory of another character -> attempt to get the item from there
                     wasPut = character.SelectedBy.Inventory.TryPutItem(item, Character.Controlled, item.AllowedSlots, true);
                }
                else if (allowEquip) //doubleclicked and no other inventory is selected
                {
                    //not equipped -> attempt to equip
                    if (!character.HasEquippedItem(item))
                    {
                        //attempt to put in a free slot first
                        for (int i = 0; i < capacity; i++)
                        {
                            if (Items[i] != null) continue;
                            if (SlotTypes[i] == InvSlotType.Any || !item.AllowedSlots.Any(a => a.HasFlag(SlotTypes[i]))) continue;
                            wasPut = TryPutItem(item, i, true, false, Character.Controlled, true);
                            if (wasPut) break;
                        }

                        if (!wasPut)
                        {
                            for (int i = 0; i < capacity; i++)
                            {
                                if (SlotTypes[i] == InvSlotType.Any || !item.AllowedSlots.Any(a => a.HasFlag(SlotTypes[i]))) continue;
                                //something else already equipped in the slot, attempt to unequip it
                                if (Items[i] != null && Items[i].AllowedSlots.Contains(InvSlotType.Any))
                                {
                                    TryPutItem(Items[i], Character.Controlled, new List<InvSlotType>() { InvSlotType.Any }, true);
                                }
                                wasPut = TryPutItem(item, i, true, false, Character.Controlled, true);
                                if (wasPut) break;
                            }
                        }
                    }
                    //equipped -> attempt to unequip
                    else if (item.AllowedSlots.Contains(InvSlotType.Any))
                    {
                        wasPut = TryPutItem(item, Character.Controlled, new List<InvSlotType>() { InvSlotType.Any }, true);
                    }
                    else
                    {
                        //cannot unequip, drop?
                        //maybe make only some items droppable so you don't accidentally drop diving suits or artifacts?
                    }
                }
            }

            if (wasPut)
            {
                for (int i = 0; i < capacity; i++)
                {
                    if (Items[i] == item) slots[i].ShowBorderHighlight(Color.Green, 0.1f, 0.9f);
                }
            }

            draggingItem = null;
            GUI.PlayUISound(wasPut ? GUISoundType.PickItem : GUISoundType.PickItemFail);
        }
        
        public void DrawOwn(SpriteBatch spriteBatch)
        {
            if (!AccessibleWhenAlive && !character.IsDead) return;
            if (slots == null) CreateSlots();
            if (GameMain.GraphicsWidth != screenResolution.X ||
                GameMain.GraphicsHeight != screenResolution.Y ||
                prevUIScale != UIScale)
            {
                SetSlotPositions(alignment);
                prevUIScale = UIScale;
            }

            base.Draw(spriteBatch);

            /*if (character == Character.Controlled)
            {
                for (int i = 0; i < capacity; i++)
                {
                    if ((selectedSlot == null || selectedSlot.SlotIndex != i) &&
                        Items[i] != null && Items[i].CanUseOnSelf && character.HasSelectedItem(Items[i]))
                    {
                        useOnSelfButton[i - 3].DrawManually(spriteBatch);
                    }
                }
            }*/


            for (int i = 0; i < capacity; i++)
            {
                if (HideSlot(i)) continue;

                if (Items[i] != null && Owner == Character.Controlled && Items[i].AllowedSlots.Any(a => a != InvSlotType.Any))
                {
                    Color color = slots[i].EquipButtonState == GUIComponent.ComponentState.Hover ? Color.White : Color.White * 0.8f;
                    if (slots[i].EquipButtonState == GUIComponent.ComponentState.Pressed) color = Color.Gray;
                    
                    EquipIndicator.Draw(spriteBatch, slots[i].EquipButtonRect.Center.ToVector2(), color, EquipIndicator.size / 2, 0, UIScale);
                    if (character.HasEquippedItem(Items[i]))
                    {
                        EquipIndicatorOn.Draw(spriteBatch, slots[i].EquipButtonRect.Center.ToVector2(), color * 0.9f, EquipIndicatorOn.size / 2, 0, UIScale * 0.85f);
                    }
                }

                /*ItemContainer container = Items[i]?.GetComponent<ItemContainer>();
                if (container != null && container.Capacity == 1 && container.Inventory.slots != null)
                {
                    container.Inventory.slots[0].SlotSprite = slotSpriteRound;
                    DrawSlot(spriteBatch, this, container.Inventory.slots[0], container.Inventory.Items[0]);
                }*/
            }

            if (CharacterHealth.OpenHealthWindow != null) return;

            if (Alignment == Alignment.Center)
            {
                toggleArrow.Draw(spriteBatch, 
                    slots[toggleArrowSlotIndex].DrawOffset + new Vector2(slots[toggleArrowSlotIndex].Rect.Center.X, slots[toggleArrowSlotIndex].Rect.Y - 50), 
                    Color.White * arrowAlpha, hidden ? 0 : MathHelper.Pi);
            }
        
        }
    }
}
