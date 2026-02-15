using Discounts.Domain.Entities.Business;
using Discounts.Persistance.Data;

namespace Discounts.Infrastructure.Data;

public static class CategorySeeder
{
    public static async Task SeedCategoriesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!context.Categories.Any())
        {
            var categories = new List<Category>
            {
                new Category
                {
                    Name = "რესტორნები",
                    Description = "ფასდაკლებები რესტორნებში და კაფეებში",
                    IconClass = "fas fa-utensils",
                    IsActive = true
                },
                new Category
                {
                    Name = "სილამაზე და ჯანმრთელობა",
                    Description = "სილამაზის სალონები, SPA, ფიტნეს ცენტრები",
                    IconClass = "fas fa-spa",
                    IsActive = true
                },
                new Category
                {
                    Name = "გართობა",
                    Description = "კინო, თეატრი, კონცერტები, ღონისძიებები",
                    IconClass = "fas fa-ticket-alt",
                    IsActive = true
                },
                new Category
                {
                    Name = "მაღაზიები",
                    Description = "ფასდაკლებები სხვადასხვა მაღაზიებში",
                    IconClass = "fas fa-shopping-bag",
                    IsActive = true
                },
                new Category
                {
                    Name = "მოგზაურობა",
                    Description = "სასტუმროები, ტურები, ავიაბილეთები",
                    IconClass = "fas fa-plane",
                    IsActive = true
                },
                new Category
                {
                    Name = "განათლება",
                    Description = "კურსები, ტრენინგები, სემინარები",
                    IconClass = "fas fa-graduation-cap",
                    IsActive = true
                },
                new Category
                {
                    Name = "სერვისები",
                    Description = "სხვადასხვა სახის სერვისები",
                    IconClass = "fas fa-concierge-bell",
                    IsActive = true
                },
                new Category
                {
                    Name = "ტექნოლოგია",
                    Description = "ელექტრონიკა, გაჯეტები, პროგრამული უზრუნველყოფა",
                    IconClass = "fas fa-laptop",
                    IsActive = true
                }
            };

            await context.Categories.AddRangeAsync(categories).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
