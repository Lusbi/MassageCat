
namespace GameCore.Attribute
{
    public abstract class AttributeBase
    {
        public abstract AttributeType attributeType { get; }

        public float currentValue { get; private set; }

        protected Attributes attributes;

        public void Initialize()
        {

        }

        public void Setup(Attributes attributes)
        {
            this.attributes = attributes;
        }

        public virtual void AddCurrentValue(float value)
        {
            currentValue += value;
        }

        public virtual void SetCurrentValue(float value)
        {
            currentValue = value;
        }

        public virtual float GetValueFormula() { return currentValue; }

        public float Get(AttributeType attributeType)
        {
            return attributes.GetAttributeValue(attributeType);
        }
    }
}