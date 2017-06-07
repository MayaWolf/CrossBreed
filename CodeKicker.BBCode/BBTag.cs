using System;

namespace CodeKicker.BBCode
{
    public class BBTag
    {
        public const string ContentPlaceholderName = "content";

        public BBTag(string name, BBTagClosingStyle tagClosingClosingStyle, bool enableIterationElementBehavior, params BBAttribute[] attributes)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (!Enum.IsDefined(typeof(BBTagClosingStyle), tagClosingClosingStyle)) throw new ArgumentException("tagClosingClosingStyle");

            Name = name;
            TagClosingStyle = tagClosingClosingStyle;
            EnableIterationElementBehavior = enableIterationElementBehavior;
            Attributes = attributes ?? new BBAttribute[0];
        }
        
        public BBTag(string name, BBTagClosingStyle tagClosingClosingStyle, params BBAttribute[] attributes)
            : this(name, tagClosingClosingStyle, false, attributes)
        {
        }

        public BBTag(string name, bool requireClosingTag, params BBAttribute[] attributes)
            : this(name, requireClosingTag ? BBTagClosingStyle.RequiresClosingTag : BBTagClosingStyle.AutoCloseElement, attributes)
        {
        }

        public BBTag(string name, params BBAttribute[] attributes)
            : this(name, true, attributes)
        {
        }

        public string Name { get; private set; }
		public bool StopProcessing { get; set; }
		public bool GreedyAttributeProcessing { get; set; }
		public bool SuppressFirstNewlineAfter { get; set; }
		public bool EnableIterationElementBehavior { get; set; }
        public bool RequiresClosingTag
        {
            get { return TagClosingStyle == BBTagClosingStyle.RequiresClosingTag; }
        }
        public BBTagClosingStyle TagClosingStyle { get; private set; }
        public BBAttribute[] Attributes { get; private set; }

        public BBAttribute FindAttribute(string name)
        {
            return Array.Find(Attributes, a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public enum BBTagClosingStyle
    {
        RequiresClosingTag = 0,
        AutoCloseElement = 1,
        LeafElementWithoutContent = 2, //leaf elements have no content - they are closed immediately
    }
}