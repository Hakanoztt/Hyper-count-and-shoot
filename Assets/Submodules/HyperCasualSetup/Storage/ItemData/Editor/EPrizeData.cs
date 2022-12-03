using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace Mobge.HyperCasualSetup {

    public class EPrizeData {
        private static LayoutRectSource s_rectSource = new LayoutRectSource();
        //private LayoutRectSource _layoutRectSource = new LayoutRectSource();
        

        public static PrizeData PrizeField(LayoutRectSource l, PrizeData gift) {
            gift.giftType = (PrizeData.Type)EditorGUI.EnumPopup(l.NextRect(), "Type", gift.giftType);
            gift.valueRankMultiplayer = EditorGUI.FloatField(l.NextRect(), "Amount Rank Multiplayer", gift.valueRankMultiplayer);
            gift.label = EditorGUI.TextField(l.NextRect(), "Label", gift.label);
            gift.labelIsFormat = EditorGUI.Toggle(l.NextRect(), "Label Is Format", gift.labelIsFormat);
            switch (gift.giftType) {
                default:
                case PrizeData.Type.Score:
                    gift.set = null;
                    gift.value = EditorGUI.FloatField(l.NextRect(), "Score", gift.value);
                    break;
                case PrizeData.Type.ItemWithQuantity:
                    gift.set = EditorDrawer.ObjectField(l.NextRect(), "Item Set", gift.set);
                    if (gift.set != null) {
                        gift.itemId = EditorDrawer.Popup(l, "Item Id", gift.set.items, gift.itemId);
                    }
                    gift.value = EditorGUI.FloatField(l.NextRect(), "Amount", gift.value);
                    break;
                case PrizeData.Type.SingleItem:
                    gift.set = EditorDrawer.ObjectField(l.NextRect(), "Item Set", gift.set);
                    if (gift.set != null) {
                        gift.itemId = EditorDrawer.Popup(l, "Item Id", gift.set.items, gift.itemId);
                    }
                    break;
            }
            return gift;
        }


        public static PrizeData PrizeFieldLayout(PrizeData gift) {
            s_rectSource.ResetInLayout();
            gift = PrizeField(s_rectSource, gift);
            s_rectSource.ConvertToLayout();
            return gift;
        }
    }
}
