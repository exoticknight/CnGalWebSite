﻿using CnGalWebSite.DataModel.Model;

using System;
 using System.Collections.Generic;
namespace CnGalWebSite.DataModel.ViewModel.Favorites
{
    public class AddFavoriteObjectViewModel
    {
        public long[] FavoriteFolderIds { get; set; }

        public FavoriteObjectType Type { get; set; }

        public long ObjectId { get; set; }
    }
}
