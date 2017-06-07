using System;

namespace CodeKicker.BBCode
{
    public class BBAttribute
    {
        public BBAttribute(string id, string name)
        {
			if(id == null) throw new ArgumentNullException("id");
			if(name == null) throw new ArgumentNullException("name");

			ID = id;
			Name = name;
		}

        public string ID { get; private set; } //ID is used to reference the attribute value
        public string Name { get; private set; } //Name is used during parsing
    }
}
