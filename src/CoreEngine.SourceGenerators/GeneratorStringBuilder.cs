using System.Text;

namespace CoreEngine.SourceGenerators
{
    public class GeneratorStringBuilder
    {
        private int indentationLevel;
        private readonly StringBuilder stringBuilder;

        public GeneratorStringBuilder()
        {
            this.stringBuilder = new StringBuilder();
        }

        public void IncreaseIndentation()
        {
            this.indentationLevel++;
        }

        public void DecreaseIndentation()
        {
            this.indentationLevel--;
        }

        public void Append(string value, bool hasIdentation = true)
        {
            if (hasIdentation)
            {
                IndentCode();
            }

            this.stringBuilder.Append(value);
        }

        public void AppendLine(string value, bool hasIdentation = true)
        {
            if (value == "}")
            {
                DecreaseIndentation();
            }

            if (hasIdentation)
            {
                IndentCode();
            }

            this.stringBuilder.AppendLine(value);

            if (value == "{")
            {
                IncreaseIndentation();
            }
        }

        public void AppendLine()
        {
            this.stringBuilder.AppendLine();
        }

        public override string ToString()
        {
            return this.stringBuilder.ToString();
        }

        private void IndentCode()
        {
            for (var i = 0; i < this.indentationLevel; i++)
            {
                this.stringBuilder.Append("    ");
            }
        }
    }
}