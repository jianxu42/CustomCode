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
