public class Script : ScriptBase
{
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        string realOperationId = this.Context.OperationId;

        // If base64 decoding is needed, uncomment and use the following code
        // try
        // {
        //     byte[] data = Convert.FromBase64String(this.Context.OperationId);
        //     realOperationId = System.Text.Encoding.UTF8.GetString(data);
        // }
        // catch (FormatException)
        // {
        //     // Not a base64 encoded string, use the original OperationId
        //     realOperationId = this.Context.OperationId;
        // }

        // Check if the operation ID matches what is specified in the OpenAPI definition of the connector
        if (realOperationId == "RegexIsMatch")
        {
            return await this.HandleRegexIsMatchOperation().ConfigureAwait(false);
        }

        // Handle an invalid operation ID
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = CreateJsonContent($"Unknown operation ID '{this.Context.OperationId}'")
        };
        return response;
    }

    private async Task<HttpResponseMessage> HandleRegexIsMatchOperation()
    {
        HttpResponseMessage response;
        try
        {
            // Read the content as a string
            var contentAsString = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);
            // Parse the JSON object
            var contentAsJson = JObject.Parse(contentAsString);

            // Get the values of textToCheck and pattern
            var textToCheck = (string)contentAsJson["textToCheck"];
            var regexInput = (string)contentAsJson["pattern"];

            // Validate inputs
            if (string.IsNullOrEmpty(textToCheck) || string.IsNullOrEmpty(regexInput))
            {
                throw new ArgumentException("Both textToCheck and pattern must be provided.");
            }

            // Validate the regex pattern
            Regex rx;
            try
            {
                // Create a Regex object
                rx = new Regex(regexInput);
            }
            catch (ArgumentException ex)
            {
                // Handle invalid regex pattern
                return CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid regex pattern: {ex.Message}");
            }

            // Match the text with the regex
            var matches = rx.Matches(textToCheck);

            if (matches.Count > 0)
            {
                // Capture matched data
                JArray matchedData = new JArray();
                foreach (Match match in matches)
                {
                    // Check if there are capturing groups
                    if (match.Groups.Count > 1)
                    {
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            matchedData.Add(match.Groups[i].Value);
                        }
                    }
                    else
                    {
                        // No capturing groups, add the whole match
                        matchedData.Add(match.Value);
                    }
                }

                // Create output JSON
                JObject output = new JObject
                {
                    ["textToCheck"] = textToCheck,
                    ["matchedCount"] = matches.Count,
                    ["matchedData"] = matchedData
                };

                // Create the response with status OK
                response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = CreateJsonContent(output.ToString())
                };
            }
            else
            {
                // Handle no matches found
                response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = CreateJsonContent("{\"message\": \"No matches found.\"}")
                };
            }
        }
        catch (JsonException jsonEx)
        {
            // Handle JSON parsing exceptions
            response = CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid JSON format: {jsonEx.Message}");
        }
        catch (ArgumentException argEx)
        {
            // Handle argument exceptions
            response = CreateErrorResponse(HttpStatusCode.BadRequest, argEx.Message);
        }
        catch (Exception ex)
        {
            // Handle general exceptions
            response = CreateErrorResponse(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
        }

        return response;
    }

    private HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string message)
    {
        JObject errorOutput = new JObject
        {
            ["error"] = message
        };
        return new HttpResponseMessage(statusCode)
        {
            Content = CreateJsonContent(errorOutput.ToString())
        };
    }

    private HttpContent CreateJsonContent(string content)
    {
        return new StringContent(content, System.Text.Encoding.UTF8, "application/json");
    }
}
