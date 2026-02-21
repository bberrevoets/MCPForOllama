# Local Testing Guide

Steps to test the MCPForOllama server on your local machine with Ollama and OpenWebUI.

> This guide has been verified working with OpenWebUI running in Docker on Windows.

## 1. Build and Start the Server

```bash
cd D:\Repos\Learning\MCPForOllama
dotnet build
dotnet run --project src/MCPForOllama.Server
```

You should see structured Serilog output in the console indicating the server is starting on `http://0.0.0.0:5000`.

## 2. Verify the Health Endpoint

Open a new terminal and run:

```bash
curl http://localhost:5000/health
```

Expected response:

```json
{"status":"healthy","service":"MCPForOllama"}
```

## 3. Verify the MCP Endpoint

Test the MCP initialize handshake directly.

> **Note:** On Windows, use `curl.exe` (not `curl`) to avoid PowerShell's `Invoke-WebRequest` alias which strips the JSON body. Both the `Accept: application/json` and `Accept: text/event-stream` types are required.

```bash
# Step 1: Initialize and get a session ID
curl.exe -i -s -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-03-26\",\"capabilities\":{},\"clientInfo\":{\"name\":\"test\",\"version\":\"1.0\"}}}"
```

Look for the `Mcp-Session-Id` header in the response — you'll need it for subsequent requests.

```bash
# Step 2: Send initialized notification (required before calling tools)
curl.exe -s -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -H "Mcp-Session-Id: YOUR_SESSION_ID" \
  -d "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}"

# Step 3: List available tools (names are snake_case)
curl.exe -s -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -H "Mcp-Session-Id: YOUR_SESSION_ID" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\"}"

# Step 4: Call a tool
curl.exe -s -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -H "Mcp-Session-Id: YOUR_SESSION_ID" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"generate_random_number\",\"arguments\":{\"min\":1,\"max\":50}}}"
```

Expected response for tool call:

```
event: message
data: {"result":{"content":[{"type":"text","text":"37"}]},"id":3,"jsonrpc":"2.0"}
```

## 4. Set Up Netatmo Weather Tool (Optional)

If you have Netatmo weather devices, set up the tool to get temperature and humidity readings.

### Prerequisites

1. Create a Netatmo developer app at [dev.netatmo.com/apps](https://dev.netatmo.com/apps)
2. Set the redirect URI to `http://localhost:5000/netatmo/callback`
3. Store your credentials:

```bash
dotnet user-secrets set "Netatmo:ClientId" "YOUR_CLIENT_ID" --project src/MCPForOllama.Server
dotnet user-secrets set "Netatmo:ClientSecret" "YOUR_CLIENT_SECRET" --project src/MCPForOllama.Server
```

### Authorize

1. With the server running, open `http://localhost:5000/netatmo/auth` in your browser
2. Log in to your Netatmo account and grant access
3. You should see: `{"status":"authenticated","message":"Netatmo tokens stored successfully. You can close this page."}`

### Test via curl

After completing OAuth setup, test the Netatmo tools via the MCP endpoint (use the session ID from step 3):

```bash
# Get current readings
curl.exe -s -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -H "Mcp-Session-Id: YOUR_SESSION_ID" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":4,\"method\":\"tools/call\",\"params\":{\"name\":\"get_temperatures\",\"arguments\":{}}}"

# Get historical data for a specific module
curl.exe -s -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -H "Mcp-Session-Id: YOUR_SESSION_ID" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":5,\"method\":\"tools/call\",\"params\":{\"name\":\"get_historical_data\",\"arguments\":{\"moduleName\":\"Outdoor\",\"hoursBack\":6}}}"
```

Expected response for `get_temperatures`:

```
event: message
data: {"result":{"content":[{"type":"text","text":"Current readings:\n  Home - Indoor: 21.3C, 45% humidity\n  Home - Outdoor: 8.1C, 78% humidity"}]},"id":4,"jsonrpc":"2.0"}
```

## 5. Connect OpenWebUI to the MCP Server

> **Important:** If OpenWebUI runs in Docker, use `host.docker.internal` instead of `localhost`.
> Docker containers cannot reach the Windows host via `localhost` — that refers to the container itself.

1. Open OpenWebUI in your browser
2. Go to **Admin Panel** (click your profile icon, then "Admin Panel")
3. Navigate to **Settings > External Tools**
4. Click the **+** button to add a new tool connection
5. Fill in:
   - **Connection Type:** MCP Streamable HTTP
   - **URL:** `http://host.docker.internal:5000/mcp` (Docker) or `http://localhost:5000/mcp` (native)
   - **Auth:** None
   - **ID:** `mcpforollama` (or any identifier you prefer)
   - **Name:** `MCPForOllama`
   - **Access:** All users
6. Click **Save**
7. Click the **refresh icon** next to the URL — it should now connect successfully and discover the `GenerateRandomNumber` tool

## 6. Test with a Model

1. Open a **New Chat** in OpenWebUI
2. Select a model that supports tool use (e.g. `qwen2.5`, `qwen3`, `mistral-nemo`)
3. In the model's **Advanced Parameters**, set **Function Calling** to `Native`
4. Make sure the tools toggle is enabled in the chat (look for the tools/wrench icon)
5. Send a prompt that should trigger the tool, for example:

   > "Give me a random number between 1 and 50"

   > "Roll a random number"

   > "Pick a random number between 200 and 300"

6. The model should call `generate_random_number` and return the result

For the Netatmo Weather tool (after completing OAuth setup), try:

   > "What's the temperature in my house?"

   > "Show me all temperature and humidity readings"

   > "How warm is it outside?"

For the Netatmo Historical Data tool, try:

   > "Show me the temperature history for the Living Room over the last 24 hours"

   > "Give me historical data for the Outdoor module, last 6 hours"

   > "What was the temperature trend in the Bedroom this week?"

> **Important:** After restarting the server or changing tools, always **start a new chat**. Old chats cache stale tool definitions and may not call updated tools correctly.

## 7. Verify Logs

### Console Logs

In the terminal where the server is running, you should see structured Serilog output for incoming MCP requests and tool invocations, including the service name and source context.

### File Logs

Check the `logs/` directory in the project root for daily rolling log files:

```bash
ls logs/
# Example: mcpforollama-20260221.log
```

### Seq (Optional)

If you have [Seq](https://datalust.co/seq) running locally:

1. Start Seq (default: `http://localhost:5341`)
2. Open the Seq UI in your browser
3. Filter by `Service = "MCPForOllama.Server-Dev"` to see all server events
4. Tool invocations will show structured properties like `Min`, `Max`, and `Result`

To install Seq locally (Docker):

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq
```

If your Seq instance requires an API key, store it via user secrets:

```bash
dotnet user-secrets set "Serilog:WriteTo:2:Args:apiKey" "YOUR_SEQ_API_KEY" --project src/MCPForOllama.Server
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Server won't start on port 5000 | Another process may be using port 5000. Check with `netstat -ano \| findstr :5000` and stop the conflicting process, or change the port in `appsettings.json` |
| OpenWebUI "connection failed" (Docker) | Use `http://host.docker.internal:5000/mcp` instead of `http://localhost:5000/mcp`. Docker containers can't reach the host via `localhost` |
| OpenWebUI "connection failed" (native) | Make sure the server is running and the URL is exactly `http://localhost:5000/mcp` |
| Windows Firewall blocks connection | Allow port 5000 through Windows Firewall: "Allow an app through Windows Firewall" or run `netsh advfirewall firewall add rule name="MCPForOllama" dir=in action=allow protocol=TCP localport=5000` |
| Model doesn't use the tool | Not all models support tool calling reliably. Use `qwen2.5`, `qwen3`, or `mistral-nemo`. Set **Function Calling** to `Native` in the model's Advanced Parameters |
| Tool appears but isn't called | Try a more explicit prompt like "Use the generate_random_number tool to give me a number between 1 and 10" |
| Tool error after server restart | Start a **new chat** in OpenWebUI — old chats cache stale tool definitions |
| Netatmo tool returns "not authenticated" | Visit `http://localhost:5000/netatmo/auth` in your browser to complete the OAuth flow |
| Netatmo OAuth callback fails | Verify your Client ID and Client Secret are correct in user secrets. Check that the redirect URI in your Netatmo app matches `http://localhost:5000/netatmo/callback` |
| Netatmo returns empty readings | Ensure your Netatmo weather station is online and reporting data in the Netatmo app |
| PowerShell `curl` doesn't work | Use `curl.exe` instead of `curl` — PowerShell aliases `curl` to `Invoke-WebRequest` which strips the JSON body |
| `406 Not Acceptable` from MCP endpoint | Add both content types to Accept header: `-H "Accept: application/json, text/event-stream"` |
| No log files in `logs/` | Ensure the server process has write permissions to the project directory |
| Seq not receiving events | Verify Seq is running on `http://localhost:5341` and check the API key in user secrets |
