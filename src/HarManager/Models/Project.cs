using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HarManager.Models
{
    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public Guid Uid { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public DateTime LastModified { get; set; } = DateTime.Now;

        public virtual List<HarEntry> Entries { get; set; } = new();
    }
}

