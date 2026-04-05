using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GinGo;

public static class GreenterBridge
{
    private static string GetBridgePath()
    {
        // Intentar encontrar el archivo runner.php en ubicaciones comunes
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        
        // 1. En el mismo directorio (producción)
        var path = Path.Combine(baseDir, "greenter-bridge", "runner.php");
        if (File.Exists(path)) return path;

        // 2. Subiendo niveles (desarrollo: bin/Debug/net9.0-windows/ -> project root)
        var current = new DirectoryInfo(baseDir);
        while (current != null)
        {
            path = Path.Combine(current.FullName, "greenter-bridge", "runner.php");
            if (File.Exists(path)) return path;
            
            // También buscar en el nivel superior directamente si no está en greenter-bridge
            path = Path.Combine(current.FullName, "runner.php");
            if (File.Exists(path)) return path;

            current = current.Parent;
        }

        // Fallback al original si no se encuentra (para mantener compatibilidad)
        return Path.Combine(baseDir, "..", "..", "..", "..", "greenter-bridge", "runner.php");
    }

    private static readonly string BridgePath = GetBridgePath();

    public static async Task<BridgeResponse> CallAsync(object payload)
    {
        try
        {
            var jsonInput = JsonSerializer.Serialize(payload);
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "php",
                Arguments = $"\"{BridgePath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            await process.StandardInput.WriteAsync(jsonInput);
            process.StandardInput.Close();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return new BridgeResponse(false, $"Error del bridge (ExitCode {process.ExitCode}): {error}");
            }

            try
            {
                // Limpiar posibles advertencias de PHP que se hayan colado en stdout
                // Buscamos el inicio del JSON '{'
                int jsonStart = output.IndexOf('{');
                if (jsonStart >= 0)
                {
                    output = output.Substring(jsonStart);
                }

                return JsonSerializer.Deserialize<BridgeResponse>(output, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                    ?? new BridgeResponse(false, "No se pudo deserializar la respuesta del bridge.");
            }
            catch (JsonException ex)
            {
                return new BridgeResponse(false, $"Error al procesar respuesta del bridge (JSON inválido): {ex.Message}. Salida: {output}");
            }
        }
        catch (Exception ex)
        {
            return new BridgeResponse(false, $"Error al llamar al bridge: {ex.Message}");
        }
    }
}

public record BridgeResponse(
    bool Success, 
    string Message, 
    string? DocumentName = null,
    string? XmlPath = null,
    string? CdrPath = null,
    string? PdfPath = null,
    string? SunatCode = null,
    string? SunatDescription = null
);
