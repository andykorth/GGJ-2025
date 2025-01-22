using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    [BindProperty]
    public required string InputText { get; set; }
    public required string ReversedText { get; set; }

    public void OnGet()
    {
        // Display the initial page
    }

    public void OnPost()
    {
        if (!string.IsNullOrWhiteSpace(InputText))
        {
            // Reverse the string
            ReversedText = new string(InputText.Reverse().ToArray());
        }
    }
}
