using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeKicker.BBCode.SyntaxTree
{
    public sealed class TextNode : SyntaxTreeNode
    {
        public TextNode(string text)
        {
			if(text == null) throw new ArgumentNullException("text");
			Text = text;
		}

        public string Text { get; private set; }

        public override string ToBBCode()
        {
            return Text.Replace("\\", "\\\\").Replace("[", "\\[").Replace("]", "\\]");
        }
        public override string ToText()
        {
            return Text;
        }

        public override SyntaxTreeNode SetSubNodes(IEnumerable<SyntaxTreeNode> subNodes)
        {
            if (subNodes == null) throw new ArgumentNullException("subNodes");
            if (subNodes.Any()) throw new ArgumentException("subNodes cannot contain any nodes for a TextNode");
            return this;
        }
        internal override SyntaxTreeNode AcceptVisitor(SyntaxTreeVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");
            return visitor.Visit(this);
        }

        protected override bool EqualsCore(SyntaxTreeNode b)
        {
            var casted = (TextNode)b;
            return Text == casted.Text;
        }
    }
}