using System.Text.Json;

namespace ServiceRequestApp.Services
{
    public interface IGeocodingService
    {
        Task<(double? latitude, double? longitude)> GeocodeAddressAsync(string street, string city, string zipcode);
    }

    public class GeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;

        public GeocodingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(double? latitude, double? longitude)> GeocodeAddressAsync(string street, string city, string zipcode)
        {
            try
            {
                // Build address string
                var addressParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(street)) addressParts.Add(street);
                if (!string.IsNullOrWhiteSpace(city)) addressParts.Add(city);
                if (!string.IsNullOrWhiteSpace(zipcode)) addressParts.Add(zipcode);
                
                if (addressParts.Count == 0) return (null, null);

                var address = string.Join(", ", addressParts) + ", Bangladesh";
                
                // Use OpenStreetMap Nominatim API
                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"https://nominatim.openstreetmap.org/search?format=json&q={encodedAddress}&limit=1&countrycodes=bd";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "ServiceRequestApp/1.0");
                
                var response = await _httpClient.GetStringAsync(url);
                var results = JsonSerializer.Deserialize<JsonElement[]>(response);
                
                if (results.Length > 0)
                {
                    var lat = results[0].GetProperty("lat").GetString();
                    var lon = results[0].GetProperty("lon").GetString();
                    
                    if (double.TryParse(lat, out var latitude) && double.TryParse(lon, out var longitude))
                    {
                        return (latitude, longitude);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - geocoding is optional
                Console.WriteLine($"Geocoding error: {ex.Message}");
            }
            
            return (null, null);
        }
    }
}

