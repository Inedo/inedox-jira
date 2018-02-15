using System;
using System.Text;

namespace Inedo.Extensions.Jira.RestApi
{
    internal sealed class QueryString
    {
        public string Jql { get; set; }
        public int? MaxResults { get; set; }

        public QueryString()
        {
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            buffer.Append('?');
            if (this.Jql != null)
                buffer.AppendFormat("jql={0}&", Uri.EscapeDataString(this.Jql));
            if (this.MaxResults != null)
                buffer.AppendFormat("maxResults={0}&", this.MaxResults);

            return buffer.ToString();
        }
    }
}
