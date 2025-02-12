﻿using Blazored.LocalStorage;
using CnGalWebSite.DataModel.Application.Examines;
using CnGalWebSite.DataModel.Application.Helper;
using CnGalWebSite.DataModel.ViewModel.Files.Images;
using CnGalWebSite.PublicToolbox.DataRepositories;
using CnGalWebSite.PublicToolbox.PostTools;
using CnGalWebSite.Shared;
using CnGalWebSite.Shared.DataRepositories;
using CnGalWebSite.Shared.MasaComponent.Shared.Tips;
using CnGalWebSite.Shared.Provider;
using CnGalWebSite.Shared.Service;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CnGalWebSite.WebAssembly
{
    /// <summary>
    /// 
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.RootComponents.Add<App>("app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            // 增加 BootstrapBlazor 组件
            builder.Services.AddBootstrapBlazor();
            //动态修改标题
            builder.RootComponents.Add<HeadOutlet>("head::after");
            // 增加 Table Excel 导出服务
            builder.Services.AddBootstrapBlazorTableExcelExport();

            builder.Services.AddBlazoredLocalStorage();

            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped(typeof(IPageModelCatche<>), typeof(PageModelCatche<>));
            builder.Services.AddScoped<IDataCacheService, DataCatcheService>();

            builder.Services.AddScoped<IAppHelper, AppHelper>();
            builder.Services.AddScoped(x => new ExamineService());
            builder.Services.AddScoped(x => new ImagesLargeViewService());

            builder.Services.AddMasaBlazor();

            //添加工具箱
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IEntryService, EntryService>();
            builder.Services.AddScoped<IArticleService, ArticleService>();
            builder.Services.AddScoped<IImageService, ImageService>();
            //添加结构化数据
            builder.Services.AddScoped<IStructuredDataService, StructuredDataService>();
            builder.RootComponents.Add<StructuredDataTip>("head::after");

            builder.Services.BuildServiceProvider(validateScopes: false);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var host = builder.Build();

            await host.RunAsync();
        }
    }
}
