using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeKicker.BBCode.SyntaxTree
{
    public sealed class TagNode : SyntaxTreeNode
    {
        public TagNode(BBTag tag)
            : this(tag, null)
        {
        }
        public TagNode(BBTag tag, IEnumerable<SyntaxTreeNode> subNodes)
            : base(subNodes)
        {
            if (tag == null) throw new ArgumentNullException("tag");
            Tag = tag;
            AttributeValues = new Dictionary<BBAttribute, string>();
        }

        public BBTag Tag { get; private set; }
        public IDictionary<BBAttribute, string> AttributeValues { get; private set; }

        public override string ToBBCode()
        {
            var content = string.Concat(SubNodes.Select(s => s.ToBBCode()).ToArray());

            var attrs = "";
            var defAttr = Tag.FindAttribute("");
            if (defAttr != null)
            {
                if (AttributeValues.ContainsKey(defAttr))
                    attrs += "=" + AttributeValues[defAttr];
            }
            foreach (var attrKvp in AttributeValues)
            {
                if (attrKvp.Key.Name == "") continue;
                attrs += " " + attrKvp.Key.Name + "=" + attrKvp.Value;
            }
            return "[" + Tag.Name + attrs + "]" + content + "[/" + Tag.Name + "]";
        }
        public override string ToText()
        {
            return string.Concat(SubNodes.Select(s => s.ToText()).ToArray());
        }

        string TryGetValue(BBAttribute attr)
        {
            string val;
            AttributeValues.TryGetValue(attr, out val);
            return val;
        }

        public override SyntaxTreeNode SetSubNodes(IEnumerable<SyntaxTreeNode> subNodes)
        {
            if (subNodes == null) throw new ArgumentNullException("subNodes");
            return new TagNode(Tag, subNodes)
                {
                    AttributeValues = new Dictionary<BBAttribute, string>(AttributeValues),
                };
        }
        internal override SyntaxTreeNode AcceptVisitor(SyntaxTreeVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");
            return visitor.Visit(this);
        }

        protected override bool EqualsCore(SyntaxTreeNode b)
        {
            var casted = (TagNode)b;
            return
                Tag == casted.Tag &&
                AttributeValues.All(attr => casted.AttributeValues[attr.Key] == attr.Value) &&
                casted.AttributeValues.All(attr => AttributeValues[attr.Key] == attr.Value);
        }
    }
}
