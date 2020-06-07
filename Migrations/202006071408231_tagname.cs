namespace RuuviTagApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class tagname : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RuuviTags", "TagName", c => c.String(maxLength: 50));
        }
        
        public override void Down()
        {
            DropColumn("dbo.RuuviTags", "TagName");
        }
    }
}
