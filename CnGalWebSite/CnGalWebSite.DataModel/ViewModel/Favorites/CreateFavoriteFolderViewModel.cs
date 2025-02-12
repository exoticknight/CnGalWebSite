﻿using System.ComponentModel.DataAnnotations;
namespace CnGalWebSite.DataModel.ViewModel.Favorites
{
    public class CreateFavoriteFolderViewModel
    {
        [Display(Name = "名称")]
        [Required(ErrorMessage = "请输入名称")]
        public string Name { get; set; }
        [Display(Name = "简介")]
        public string BriefIntroduction { get; set; }
        [Display(Name = "是否为默认收藏夹")]
        public bool IsDefault { get; set; }

        //public string MainImage { get; set; }
    }
}
