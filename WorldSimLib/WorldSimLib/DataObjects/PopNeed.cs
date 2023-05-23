using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace WorldSimLib.DataObjects
{
    public class PopItemNeed
    {
        public string ItemName { get; set; }
        public int Qty { get; set; }
    }

    public class PopNeed
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public int MinSOLLevel { get; set; }

        public int MaxSOLLevel { get; set; }

        public List<ItemType> AssociatedItemTypes { get; set; }

        public static void Prime( GameData data, List<PopNeed> needsToPrime )
        {
           
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"PopNeed: {Name}");
            sb.AppendLine($"ID: {ID}");
            sb.AppendLine($"Min Standard Of Living (SOL) Level: {MinSOLLevel}");
            sb.AppendLine($"Max Standard Of Living (SOL) Level: {MaxSOLLevel}");

            sb.AppendLine("Associated Item Types:");
            foreach (var itemType in AssociatedItemTypes)
            {
                sb.AppendLine(itemType.ToString());
            }

            return sb.ToString();
        }

        public string ToMarkdown()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"#### PopNeed: {Name}");
            sb.AppendLine($"- ID: `{ID}`");
            sb.AppendLine($"- Min Standard Of Living (SOL) Level: `{MinSOLLevel}`");
            sb.AppendLine($"- Max Standard Of Living (SOL) Level: `{MaxSOLLevel}`");

            sb.AppendLine("##### Associated Item Types:");
            foreach (var itemType in AssociatedItemTypes)
            {
                sb.AppendLine($"- {itemType.ToString()}");
            }

            return sb.ToString();
        }

    }
}
