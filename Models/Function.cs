
using System;
using System.Collections.Generic;
using OpenAI.ObjectModels.RequestModels;

namespace achappey.ChatGPTeams.Models
{

    public class Function
    {

        // public string Title { get; set; }

        public string Title
        {
            get
            {
                return FunctionDefinition != null ? FunctionDefinition?.Description : Id;
            }
        }

        public FunctionDefinition FunctionDefinition { get; set; }

        public string Publisher { get; set; }

        public string Category { get; set; }

        public string Url { get; set; }

        public string Id { get; set; }

        public Function()
        {
        }


    }


    public class Response
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}