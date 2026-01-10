# 📚 LoR Mod Editor (废墟图书馆 Mod 编辑器)

![Version](https://img.shields.io/github/v/release/Castanea/LorModEditor?label=Version&color=blue)
![License](https://img.shields.io/github/license/Castanea/LorModEditor)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)

一个专为《Library of Ruina》BaseMod 模组制作设计的现代化、可视化编辑器。
基于 **C#**、**WPF** 和 **Prism** 架构开发，旨在让 Mod 制作变得像填表一样简单。

## ✨ 核心特性

*   **全流程覆盖**：
    *   🎴 **战斗书页**：骰子配置、特效选择、脚本关联。
    *   ⚡ **被动能力**：费用、稀有度、互斥 ID 配置。
    *   📖 **核心书页**：抗性表、属性值、专属被动/卡牌绑定。
    *   👹 **敌人单位**：卡组构建、掉落配置、撤退逻辑。
    *   🏰 **关卡配置**：波次管理、邀请函配方设置。
*   **智能辅助**：
    *   🔍 **自动扫描**：自动读取 Mod 目录下的图片和 DLL 脚本，提供下拉选择。
    *   🤖 **原版数据**：支持加载 BaseMod 原版数据，方便引用原版被动/卡牌。
    *   🚑 **项目体检**：一键扫描 ID 引用错误，防止进游戏红字。
    *   📄 **设计导出**：一键生成 Markdown 格式的设计文档，方便与策划交流。
*   **强类型安全**：底层采用强类型校验，防止 XML 拼写错误导致的 Bug。

确保安装了 `.NET 10.0` SDK。

## 🤝 贡献与反馈
欢迎提交 **Issue** 反馈 Bug 或建议新功能。
如果您想贡献代码，请 Fork 本仓库并提交 **Pull Request**。

**注意：** 本项目架构已迁移至 **Prism** 模块化设计。
*   核心逻辑位于 `LorModEditor.Core`。
*   界面逻辑位于 `LorModEditor` (Views/ViewModels)。

## 📜 许可证 (License)
本项目采用 **GPL v3.0** 协议开源。
您可以自由地使用、修改和分发本项目，但 **您的修改版本也必须开源**，并保留原作者署名。

---
*Created with ❤️ by Castanea*
