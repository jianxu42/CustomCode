public class Script : ScriptBase
{
	public override async Task<HttpResponseMessage> ExecuteAsync()
	{
		// Check if the operation ID matches what is specified in the OpenAPI definition of the connector
		if (this.Context.OperationId == "RegexIsMatch")
		{
			return await this.HandleRegexIsMatchOperation().ConfigureAwait(false);
		}

		// Handle an invalid operation ID
		HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.BadRequest);
		response.Content = CreateJsonContent($"Unknown operation ID '{this.Context.OperationId}'");
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
			// Validate inputs
			if (string.IsNullOrEmpty(textToCheck) || string.IsNullOrEmpty(regexInput))
			{
				throw new ArgumentException("Both textToCheck and regex must be provided.");
			}

			// Create a Regex object
			var rx = new Regex(regexInput);
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
		catch (Exception ex)
		{
			// Handle exceptions and return a bad request response
			JObject errorOutput = new JObject
			{
				["error"] = ex.Message
			};
			response = new HttpResponseMessage(HttpStatusCode.BadRequest)
			{
				Content = CreateJsonContent(errorOutput.ToString())
			};
		}

		return response;
	}
}
