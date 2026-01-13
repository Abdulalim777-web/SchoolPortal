using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolPortal.Data;

#nullable disable

namespace SchoolPortal.Migrations
{
    [DbContext(typeof(SchoolPortalDbContext))]
    [Migration("20260112120000_AddRrrNumberToPayment")]
    partial class AddRrrNumberToPayment
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Migration designer placeholder. Model snapshot contains the canonical model.
        }
    }
}
