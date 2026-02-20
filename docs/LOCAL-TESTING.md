# Local Testing Guide

Steps to test the MCPForOllama server on your local machine with Ollama and OpenWebUI.

> This guide has been verified working with OpenWebUI running in Docker on Windows.

## 1. Build and Start the Server

```bash
cd D:\Repos\Learning\MCPForOllama
dotnet build
dotnet run --project src/MCPForOllama.Server
```

You should see output indicating the server is listening on `http://0.0.0.0:5000`.

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

Test the MCP initialize handshake directly:

```bash
curl -s -X POST http://localhost:5000/mcp -H "Content-Type: application/json" -d "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-03-26\",\"capabilities\":{},\"clientInfo\":{\"name\":\"test\",\"version\":\"1.0\"}}}"
```

Expected response (SSE format):

```
event: message
data: {"result":{"protocolVersion":"2025-03-26","capabilities":{"logging":{},"tools":{"listChanged":true}},"serverInfo":{"name":"MCPForOllama.Server","version":"1.0.0.0"}},"id":1,"jsonrpc":"2.0"}
```

## 4. Connect OpenWebUI to the MCP Server

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

## 5. Test with a Model

1. Open a **New Chat** in OpenWebUI
2. Select a model that supports tool use (e.g. `llama3.1`, `qwen2.5`, `mistral`)
3. Make sure the tools toggle is enabled in the chat (look for the tools/wrench icon)
4. Send a prompt that should trigger the tool, for example:

   > "Give me a random number between 1 and 50"

   > "Roll a random number"

   > "Pick a random number between 200 and 300"

5. The model should call `GenerateRandomNumber` and return the result

## 6. Verify Tool Call in Server Logs

In the terminal where the server is running, you should see log entries for incoming MCP requests when the model calls the tool.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Server won't start on port 5000 | Another process may be using port 5000. Check with `netstat -ano \| findstr :5000` and stop the conflicting process, or change the port in `appsettings.json` |
| OpenWebUI "connection failed" (Docker) | Use `http://host.docker.internal:5000/mcp` instead of `http://localhost:5000/mcp`. Docker containers can't reach the host via `localhost` |
| OpenWebUI "connection failed" (native) | Make sure the server is running and the URL is exactly `http://localhost:5000/mcp` |
| Windows Firewall blocks connection | Allow port 5000 through Windows Firewall: "Allow an app through Windows Firewall" or run `netsh advfirewall firewall add rule name="MCPForOllama" dir=in action=allow protocol=TCP localport=5000` |
| Model doesn't use the tool | Not all models support tool calling. Try `llama3.1` or `qwen2.5` which have good tool support |
| Tool appears but isn't called | Try a more explicit prompt like "Use the GenerateRandomNumber tool to give me a number between 1 and 10" |
