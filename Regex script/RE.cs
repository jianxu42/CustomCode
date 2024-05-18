public class Script : ScriptBase
{
	public override async Task<HttpResponseMessage> ExecuteAsync()
	{
		string realOperationId = this.Context.OperationId;
		// Resolve potential issue with base64 encoding of the OperationId
		// Test and decode if it's base64 encoded
		/*try
        {
            byte[] data = Convert.FromBase64String(this.Context.OperationId);
            realOperationId = System.Text.Encoding.UTF8.GetString(data);
        }
        catch (FormatException)
        {
            // Not a base64 encoded string, use the original OperationId
            realOperationId = this.Context.OperationId;
        }*/
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
			// Get the values of textToCheck and regex
			var textToCheck = (string)contentAsJson["textToCheck"];
			var regexInput = (string)contentAsJson["regex"];
			// Get the regex option (if any)
			var option = (string)contentAsJson["option"] ?? "none";
			// Validate inputs
			if (string.IsNullOrEmpty(textToCheck) || string.IsNullOrEmpty(regexInput))
			{
				throw new ArgumentException("Both textToCheck and regex must be provided.");
			}

			// Determine RegexOptions based on the option value
			RegexOptions options = option.ToLower() switch
			{
				"ignorecase" => RegexOptions.IgnoreCase,
				"multiline" => RegexOptions.Multiline,
				"singleline" => RegexOptions.Singleline,
				_ => RegexOptions.None,
			};
			// Create a Regex object with options
			var rx = new Regex(regexInput, options);
			// Match the text with the regex
			var match = rx.Match(textToCheck);
			// Capture matched data
			JArray matchedData = new JArray();
			foreach (Group group in match.Groups)
			{
				matchedData.Add(group.Value);
			}

			// Create output JSON
			JObject output = new JObject
			{
				["textToCheck"] = textToCheck,
				["isMatch"] = match.Success,
				["matchedData"] = matchedData
			};
			// Create the response with status OK
			response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = CreateJsonContent(output.ToString())
			};
		}
		catch (JsonException jsonEx)
		{
			// Handle JSON parsing exceptions
			JObject errorOutput = new JObject
			{
				["error"] = $"Invalid JSON format: {jsonEx.Message}"};
			response = new HttpResponseMessage(HttpStatusCode.BadRequest)
			{
				Content = CreateJsonContent(errorOutput.ToString())
			};
		}
		catch (ArgumentException argEx)
		{
			// Handle argument exceptions
			JObject errorOutput = new JObject
			{
				["error"] = argEx.Message
			};
			response = new HttpResponseMessage(HttpStatusCode.BadRequest)
			{
				Content = CreateJsonContent(errorOutput.ToString())
			};
		}
		catch (Exception ex)
		{
			// Handle general exceptions
			JObject errorOutput = new JObject
			{
				["error"] = $"An error occurred: {ex.Message}"};
			response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
			{
				Content = CreateJsonContent(errorOutput.ToString())
			};
		}

		return response;
	}

	private HttpContent CreateJsonContent(string content)
	{
		return new StringContent(content, System.Text.Encoding.UTF8, "application/json");
	}
}