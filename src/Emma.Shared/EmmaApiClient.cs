using System.Net.Http.Json;
using Emma.Shared.Dtos;
using Emma.Shared.Models;

namespace Emma.Shared;

/// <summary>
/// Dünner HTTP-Client für den zentralen Emma.Service. Wird sowohl von der
/// TrayApp (Benutzerseite) als auch vom Viewer (EMMA-Fenster) verwendet.
/// Prüft das selbstsignierte Server-Zertifikat per Thumbprint-Pinning (statt
/// den Windows-Zertifikatsspeicher zu verändern) und sendet den API-Key mit.
/// </summary>
public class EmmaApiClient
{
    private readonly HttpClient _http;

    public EmmaApiClient(EmmaClientConfig config)
    {
        var handler = new HttpClientHandler();

        if (!string.IsNullOrWhiteSpace(config.ZertifikatThumbprint))
        {
            var erwarteterThumbprint = config.ZertifikatThumbprint.Replace(" ", "").ToUpperInvariant();
            handler.ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
                cert is not null && string.Equals(cert.Thumbprint, erwarteterThumbprint, StringComparison.OrdinalIgnoreCase);
        }

        _http = new HttpClient(handler) { BaseAddress = new Uri(config.ServiceBaseUrl) };

        if (!string.IsNullOrWhiteSpace(config.ApiKey))
            _http.DefaultRequestHeaders.Add("X-Api-Key", config.ApiKey);
    }

    public async Task<List<ProzessDto>> GetProzesseAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<ProzessDto>>("api/prozesse", ct) ?? [];

    public async Task<ProzessDto> ErstelleProzessAsync(NeuerProzessRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/prozesse", request, ct);
        await WirfBeiFehlerAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<ProzessDto>(cancellationToken: ct))!;
    }

    public async Task<ProzessDto> AktualisiereProzessAsync(int prozessId, NeuerProzessRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/prozesse/{prozessId}", request, ct);
        await WirfBeiFehlerAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<ProzessDto>(cancellationToken: ct))!;
    }

    public async Task LoescheProzessAsync(int prozessId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/prozesse/{prozessId}", ct);
        await WirfBeiFehlerAsync(response, ct);
    }

    /// <summary>Wirft mit dem Antworttext als Meldung statt nur "400 Bad Request", damit die
    /// serverseitigen Validierungsmeldungen (z.B. "Name bereits vergeben") beim Nutzer ankommen.</summary>
    private static async Task WirfBeiFehlerAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var text = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(string.IsNullOrWhiteSpace(text) ? response.ReasonPhrase : text);
    }

    public async Task<List<AufgabeDto>> GetAufgabenAsync(AufgabeStatus? status = null, CancellationToken ct = default)
    {
        var url = status is null ? "api/aufgaben" : $"api/aufgaben?status={status}";
        return await _http.GetFromJsonAsync<List<AufgabeDto>>(url, ct) ?? [];
    }

    /// <summary>Komplette Historie (alle Status) für die Verlauf-/Dashboard-Ansicht.</summary>
    public Task<List<AufgabeDto>> GetAufgabenHistorieAsync(CancellationToken ct = default) =>
        GetAufgabenAsync(status: null, ct);

    public async Task<AufgabeDto> ErstelleAufgabeAsync(NeueAufgabeRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/aufgaben", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AufgabeDto>(cancellationToken: ct))!;
    }

    public async Task MarkiereErledigtAsync(int aufgabeId, CancellationToken ct = default)
    {
        var response = await _http.PatchAsync($"api/aufgaben/{aufgabeId}/erledigt", content: null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task MarkiereInBearbeitungAsync(int aufgabeId, CancellationToken ct = default)
    {
        var response = await _http.PatchAsync($"api/aufgaben/{aufgabeId}/in-bearbeitung", content: null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task MarkiereFehlgeschlagenAsync(int aufgabeId, string? fehlermeldung = null, CancellationToken ct = default)
    {
        var response = await _http.PatchAsJsonAsync(
            $"api/aufgaben/{aufgabeId}/fehlgeschlagen", new FehlschlagRequest(fehlermeldung), ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<WiederkehrenderPlanDto>> GetWiederkehrendePlaeneAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<WiederkehrenderPlanDto>>("api/wiederkehrende-plaene", ct) ?? [];

    public async Task<WiederkehrenderPlanDto> ErstellePlanAsync(NeuerWiederkehrenderPlanRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/wiederkehrende-plaene", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WiederkehrenderPlanDto>(cancellationToken: ct))!;
    }

    public async Task<WiederkehrenderPlanDto> AktualisierePlanAsync(int planId, NeuerWiederkehrenderPlanRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"api/wiederkehrende-plaene/{planId}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WiederkehrenderPlanDto>(cancellationToken: ct))!;
    }

    public async Task LoeschePlanAsync(int planId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/wiederkehrende-plaene/{planId}", ct);
        response.EnsureSuccessStatusCode();
    }
}
