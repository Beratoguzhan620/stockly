using System.ComponentModel.DataAnnotations;

namespace Stockly.Web.Models;

public class CategoryFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Kategori adı")]
    [Required(ErrorMessage = "Kategori adını yazın.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Kategori adı 2 ile 60 karakter arasında olmalı.")]
    public string Name { get; set; } = string.Empty;
}
