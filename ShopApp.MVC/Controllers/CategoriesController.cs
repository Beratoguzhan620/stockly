using Microsoft.AspNetCore.Mvc;
using ShopApp.MVC.Data;
using ShopApp.MVC.Data.Entities;

namespace ShopApp.MVC.Controllers;

public class CategoriesController(AppDbContext context) : Controller
{
    //TODO: create ve edit işlemlerinde mükerrer kategori adı eklenmesin, bunu kontrol edin ve uygun hata mesajını dönün: TAMAMLANDI
    //TODO: silme işlemi butona bağlansın: TAMAMLANDI
    public IActionResult Index()
    {
        List<Category> categories = context.Categories.ToList();
        return View(categories);
    }


    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Category category)
    {

        //Validation Rule Kontrolü (Validasyon kuralları)

        if (!ModelState.IsValid)
            return View();


        //Business Rule Kontrolü (İş kuralları)
        //DB'de aynı isimde bir kategori var mı kontrolü
        var categoryExists = context.Categories.Any(c => c.Name == category.Name);

        if (categoryExists)
        {
            ViewBag.ErrorMessage = "Bu isimde bir kategori zaten mevcut.";
            return View();
        }

        context.Categories.Add(category);
        context.SaveChanges();
        return RedirectToAction(nameof(Index)); //Ana sayfaya dön
    }


    public IActionResult Edit(int id)
    {
        var category = context.Categories.Find(id);

        if (category is null)
            return RedirectToAction(nameof(Index));

        return View(category);
    }

    [HttpPost]
    public IActionResult Edit(int id, Category category)
    {
        //Validation Check
        if (!ModelState.IsValid)
            return View();

        //Business Rule Kontrolü (İş kuralları)
        //DB'de diğer kategoriler arasında aynı isimde bir kategori var mı kontrolü
        var categoryExists = context.Categories.Any(c => c.Name == category.Name && c.Id != id);

        if (categoryExists)
        {
            ViewBag.ErrorMessage = "Bu isimde bir kategori zaten mevcut.";
            return View();
        }

        context.Categories.Update(category);
        context.SaveChanges();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var category = context.Categories.Find(id);

        if (category is not null)
        {
            context.Categories.Remove(category);
            context.SaveChanges();
        }

        return RedirectToAction(nameof(Index));
    }

}
