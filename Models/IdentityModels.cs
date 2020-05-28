using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace RuuviTagApp.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        [StringLength(256)]
        public override string Email { get => base.Email; set => base.Email = value; }
        [StringLength(256)]
        public override string UserName { get => base.UserName; set => base.UserName = value; }
        [StringLength(1024)]
        public override string PasswordHash { get => base.PasswordHash; set => base.PasswordHash = value; }
        [StringLength(1024)]
        public override string SecurityStamp { get => base.SecurityStamp; set => base.SecurityStamp = value; }
        [StringLength(50)]
        public override string PhoneNumber { get => base.PhoneNumber; set => base.PhoneNumber = value; }
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext() : base("LocalDBConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<RuuviTagModel> RuuviTagModels { get; set; }
        public DbSet<UserTagListModel> UserTagListModels { get; set; }
        public DbSet<TagListRowModel> TagListRowModels { get; set; }
    }
}