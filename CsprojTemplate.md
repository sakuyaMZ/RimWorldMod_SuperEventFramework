# FactionTerritories 通用 csproj 模板（可复制粘贴使用）

以下是一个基于 SDK 样式的通用 RimWorld Mod 项目文件模板，包含必要注释与可选拓展。请将其中的占位符 `xxx` 替换为你的实际路径或名称。

```xml
<?xml version="1.0" encoding="utf-8"?>
<!--
  模板说明：
  - 推荐使用 Krafs.Rimworld.Ref 作为“引用程序集”NuGet 包（仅编译期），避免手动维护 Unity/RimWorld DLL 路径。
  - 若环境无法访问 NuGet 或需固定到本地 DLL，可启用“手动引用拓展”片段。
  - 额外的NuGet查阅 https://www.nuget.org/ 按需添加
    * 搜rim等关键词或者作者id等信息基本就能找到需要的，没有就是没有，或者查作者的github或者mod的readme，创意工坊描述等，要么干脆直接找作者问 
  - 本模板使用三个独立开关：
    * UseLocalGameRefs 控制本地游戏 DLL（Assembly-CSharp/UnityEngine/0Harmony）
    * UseLocalModRefs  控制本地模组 DLL（例如 MapModeFramework 等不在 NuGet 的依赖）
    * UseNugetHarmony 控制 NuGet 版 Harmony（Lib.Harmony）
-->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- 目标框架：RimWorld 基于 .NET Framework 4.8 -->
    <TargetFramework>net48</TargetFramework>
    <!-- 语言版本：C# 10.0（支持 Nullable 引用类型） -->
    <!-- 最高到11，11也不安全，不要再高了 -->
    <LangVersion>10.0</LangVersion>
    <!-- 程序集名称（输出 DLL 名） -->
    <AssemblyName>xxx</AssemblyName>
    <!-- 根命名空间（C# 命名空间前缀） -->
    <RootNamespace>xxx</RootNamespace>
    <!-- 输出类型：类库 -->
    <OutputType>Library</OutputType>
  </PropertyGroup>

  <ItemGroup Condition="'$(UseLocalGameRefs)'!='true'">
    <!-- 推荐：使用 Krafs.Rimworld.Ref（仅用于编译；不复制到输出） -->
    <!-- NuGet 从网络仓库获取引用 -->
    <!-- 仓库名与版本，* 表示匹配该主次版本下的任意修订 -->
    <PackageReference Include="Krafs.Rimworld.Ref" Version="1.6.*">
      <!-- 编译/构建相关资产可见 -->
      <!-- 当前项目使用到此包的以下资产 -->
      <!-- compile: 编译时需要的代码 -->
      <!-- build: 构建时需要的文件 -->
      <!-- native: 本地库文件 -->
      <!-- contentfiles: 内容文件 -->
      <!-- analyzers: 代码分析器 -->
      <IncludeAssets>compile;build;native;contentfiles;analyzers</IncludeAssets>
      <!-- 排除运行时资产，避免把游戏 DLL 复制到 Mod 输出目录 -->
      <ExcludeAssets>runtime</ExcludeAssets>
      <!-- 不向下游项目传递 -->
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <!-- 可选：若不使用 NuGet 或需固定到本地 DLL，可开启以下开关并填写路径 -->
  <PropertyGroup>
    <!-- 是否使用本地游戏 DLL（Assembly-CSharp/UnityEngine/Harmony） -->
    <UseLocalGameRefs>false</UseLocalGameRefs>
    <!-- 是否使用第三方/模组（例如 MapModeFramework 等不在 NuGet 的依赖） -->
    <UseLocalModRefs>false</UseLocalModRefs>
    <!-- 是否使用 NuGet 版 Harmony（Lib.Harmony）；仅在未启用本地游戏 DLL 时可用 -->
    <UseNugetHarmony>false</UseNugetHarmony>
  </PropertyGroup>

  <!-- 按需加载：NuGet 版 Harmony（避免与本地 0Harmony.dll 重复） -->
  <ItemGroup Condition="'$(UseNugetHarmony)'=='true'">
    <PackageReference Include="Lib.Harmony" Version="2.*">
      <IncludeAssets>compile;build;analyzers</IncludeAssets>
      <ExcludeAssets>runtime</ExcludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <!-- 手动引用拓展：本地游戏 DLL（Verse/RimWorld/Unity/Harmony） -->
  <!-- 基本不用 -->
  <ItemGroup Condition="'$(UseLocalGameRefs)'=='true'">
    <!-- RimWorld 主程序集（包含 Verse/RimWorld/RimWorld.Planet） -->
    <Reference Include="Assembly-CSharp">
      <HintPath>xxx\RimWorldWin64_Data\Managed\Assembly-CSharp.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <!-- HarmonyLib（Harmony 补丁框架） -->
    <Reference Include="0Harmony">
      <HintPath>xxx\RimWorldWin64_Data\Managed\0Harmony.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <!-- Unity 引擎常用模块：根据实际 API 选用 -->
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>xxx\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="UnityEngine.IMGUIModule">
      <HintPath>xxx\RimWorldWin64_Data\Managed\UnityEngine.IMGUIModule.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="UnityEngine.TextRenderingModule">
      <HintPath>xxx\RimWorldWin64_Data\Managed\UnityEngine.TextRenderingModule.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <!-- 如用到 UI/Audio/Physics 等，再按需添加其他 UnityEngine.* 模块 -->
  </ItemGroup>

  <!-- 手动引用拓展：第三方/模组 DLL（示例：MapModeFramework） -->
  <ItemGroup Condition="'$(UseLocalModRefs)'=='true'">
    <Reference Include="MapModeFramework">
      <HintPath>xxx\Mods\MapModeFramework\Assemblies\MapModeFramework.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <!-- 其他必须手动引用的 DLL，按需追加 -->
  </ItemGroup>

  <!--
    编译后自动将 DLL 复制到 1.6/Assemblies/（RimWorld 通过 About/LoadFolders.xml 加载版本子目录）
    没有这个自动复制的话，每次编译完还得手拷过去。
   -->
  <Target Name="CopyToAssemblies" AfterTargets="Build">
    <!-- 主 DLL -->
    <Copy SourceFiles="$(OutputPath)$(AssemblyName).dll" DestinationFolder="$(ProjectDir)1.6\Assemblies" />
    <!-- Harmony DLL（如果输出目录中存在则一并复制） -->
    <Copy SourceFiles="$(OutputPath)0Harmony.dll" DestinationFolder="$(ProjectDir)1.6\Assemblies" Condition="Exists('$(OutputPath)0Harmony.dll')" />
  </Target>
</Project>
```

使用指南：
- 仅使用 NuGet：保持 `<UseLocalGameRefs>false</UseLocalGameRefs>`，按需将 `<UseNugetHarmony>true</UseNugetHarmony>`，然后执行包还原。
- 使用本地游戏 DLL：将 `<UseLocalGameRefs>true</UseLocalGameRefs>`，并把 `xxx` 路径替换为你本地的 RimWorld 安装目录。
- 使用本地模组 DLL：将 `<UseLocalModRefs>true</UseLocalModRefs>`，并把 `xxx` 路径替换为依赖 Mod 的 Assemblies 路径（可与 NuGet 并用）。
- Harmony 选择策略：
  - 若 `<UseLocalGameRefs>true</UseLocalGameRefs>`，使用本地 0Harmony.dll。
  - 若 `<UseLocalGameRefs>false</UseLocalGameRefs>` 且需要 Harmony，请将 `<UseNugetHarmony>true</UseNugetHarmony>`（引入 Lib.Harmony 包）。
- 编译后自动复制（`<Target Name="CopyToAssemblies" AfterTargets="Build">`）：
  - 编译成功后自动将 DLL 从 `bin\Release\net48\` 复制到 `Assemblies\`。
  - RimWorld 加载 Mod DLL 的标准路径是 `Assemblies/`，此步骤省去手工拷贝。
  - `0Harmony.dll` 仅当输出目录中存在时才复制（条件：`Condition="Exists(...)"`）。
- Unity 引擎模块：
  - 使用 NuGet Krafs.Rimworld.Ref 时无需手动添加 UnityEngine 模块。
  - 使用本地游戏 DLL 时，如报缺少引擎类型，按需在"手动引用拓展"中追加对应的 UnityEngine.* 模块（常见：CoreModule、IMGUIModule、TextRenderingModule）。
