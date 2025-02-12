﻿using CnGalWebSite.DataModel.Helper;
using CnGalWebSite.DataModel.Model;
using CnGalWebSite.Helper.Helper;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace CnGalWebSite.RobotClient
{
    public class GroupX
    {

        public List<RobotGroup> Groups { get; set; } = new List<RobotGroup>();
        private readonly Setting _setting;
        private readonly HttpClient _httpClient;


        public GroupX(Setting setting, HttpClient client)
        {
            _setting = setting;
            _httpClient = client;
        }


        public void Init()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                var path = Path.Combine(_setting.RootPath, "Groups.json");

                using (var file = File.OpenText(path))
                {
                    var serializer = new JsonSerializer();
                    Groups = (List<RobotGroup>)serializer.Deserialize(file, typeof(List<RobotGroup>));
                    file.Close();
                    file.Dispose();
                }
            }
            catch
            {
                Save();
            }

        }

        public void Save()
        {
            var path = Path.Combine(_setting.RootPath, "Groups.json");

            using (var file = File.CreateText(path))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(file, Groups);
                file.Close();
                file.Dispose();
            }
        }

        public async Task RefreshAsync()
        {
            try
            {
                var model = await _httpClient.GetFromJsonAsync<List<RobotGroup>>(ToolHelper.WebApiPath + "api/robot/getrobotGroups");
                Groups.Clear();
                Groups.AddRange(model);
                Save();
            }
            catch (Exception ex)
            {
                OutputHelper.PressError(ex, "无法获取QQ群列表");
            }
        }
    }
}
