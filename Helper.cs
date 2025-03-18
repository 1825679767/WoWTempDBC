
using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;

namespace WoWTempDBC
{
    public static class Helper
    {
        /// <summary>
        /// 定位的DBC目录
        /// </summary>
        public static string SaveDBCPath { get; set; } = string.Empty;

        /// <summary>
        /// 内置的DBC目录 用于转存
        /// </summary>
        public static readonly string DataDBCOnePath = Application.StartupPath + "\\Dream-Az-OneData-2022-11-25-10-00-FAEE37B8D1C5A53EA0B93384C3ADFF63\\";

        /// <summary>
        /// 初始的头
        /// </summary>
        public static readonly string ViewItemTitle = "===== 全选|取消 =====";

        /// <summary>
        /// 自定义的数据库名头
        /// </summary>
        public static readonly string CustomTitle = "__________数据-";

        /// <summary>
        /// 临时文件目录
        /// </summary>
        public static readonly string TempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DataTempDBCSQL");

        /// <summary>
        /// XLS目录
        /// </summary>
        public static readonly string XLSFolder = Application.StartupPath + "\\Xlsx\\";

        /// <summary>
        /// XML
        /// </summary>
        public static readonly string XMLPath = DataDBCOnePath + "TableField.xml";

        /// <summary>
        /// 排除的
        /// </summary>
        public static readonly string[] PListDbc = { "WorldStateZoneSounds.dbc", "WorldChunkSounds.dbc", "TerrainType.dbc", "ItemSubClassMask.dbc", "ItemSubClass.dbc", "ItemClass.dbc", "gtRegenMPPerSpt.dbc", "gtRegenHPPerSpt.dbc", "gtOCTRegenMP.dbc", "gtOCTRegenHP.dbc", "gtNPCManaCostScaler.dbc", "gtCombatRatings.dbc", "gtChanceToSpellCritBase.dbc", "gtChanceToSpellCrit.dbc", "gtChanceToMeleeCritBase.dbc", "gtChanceToMeleeCrit.dbc", "gtBarberShopCostBase.dbc", "GameTables.dbc", "CharBaseInfo.dbc", "CharacterFacialHairStyles.dbc", "Cfg_Configs.dbc", "Cfg_Categories.dbc", "AttackAnimTypes.dbc", "AttackAnimKits.dbc", "AnimationData.dbc" };

        /// <summary>
        /// 模态提示框
        /// <returns>返回模态的DialogResult值</returns>
        /// </summary>
        public static DialogResult BoxShow(Form ParentForm, string MText, string WinText, BtnTypes BType = BtnTypes.OK)
        {
            InfoBox DoForm = new InfoBox(ParentForm, MText, WinText, BType);
            return DoForm.ShowDialog();
        }
    }
}