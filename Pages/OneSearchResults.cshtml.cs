using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Authorization;

namespace Khronos4.Pages
{
    [Authorize]
    public class ResultsModel : PageModel
    {
        [BindProperty(SupportsGet = true)] public string Street { get; set; }
        [BindProperty(SupportsGet = true)] public string City { get; set; }
        [BindProperty(SupportsGet = true)] public string State { get; set; }
        [BindProperty(SupportsGet = true)] public string Sites { get; set; }

        public List<string> SearchUrls { get; private set; } = new();

        public void OnGet()
        {
            var selectedSites = Sites.Split(',').ToList();

            var formattedStreet = Street.ToLower().Replace(" ", "-");
            var formattedCity = City.ToLower().Replace(" ", "-");
            var formattedState = State.ToLower();

            var searchUrls = new Dictionary<string, string>
            {
                { "fps", $"https://www.fastpeoplesearch.com/address/{formattedStreet}_{formattedCity}-{formattedState}" },
                { "spf", $"https://www.searchpeoplefree.com/address/{formattedState}/{formattedCity}/{FormatSearchPeopleFree(Street)}" },
                { "wps", $"https://www.whitepages.com/address/{formattedStreet}/{formattedCity}-{formattedState}" },
                { "tht", $"https://thatsthem.com/address/{formattedStreet}.-{formattedCity}-{formattedState}" },
                { "tps", $"https://www.truepeoplesearch.com/resultaddress?streetaddress={HttpUtility.UrlEncode(Street)}&citystatezip={HttpUtility.UrlEncode(City)}+{formattedState.ToUpper()}" },
                { "sbc", $"https://www.smartbackgroundchecks.com/address-search/{formattedStreet}/{formattedCity}/{formattedState}" }
            };

            SearchUrls = selectedSites.Where(searchUrls.ContainsKey).Select(site => searchUrls[site]).ToList();
        }

        private string FormatStreet(string street)
        {
            street = street.ToLower().Replace(".", "").Replace(" ", "-");
            return street;
        }

        private string FormatCity(string city)
        {
            return city.ToLower().Replace(" ", "-");
        }

        private string FormatSearchPeopleFree(string street)
        {
            var parts = street.Split(' ');
            if (parts.Length > 1)
            {
                string streetNumber = parts[0];  // First part is the street number
                string streetName = string.Join("-", parts.Skip(1));  // Join the rest of the parts
                return $"{streetName}/{streetNumber}";  // Correct format: keswick-st/21042
            }
            return street.ToLower().Replace(" ", "-"); // Fallback
        }
    }
}
