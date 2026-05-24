using System.Runtime.CompilerServices;

// 允许测试项目访问 internal 成员，以便在不暴露生产 API 的前提下完成行为验证。
[assembly: InternalsVisibleTo("DispensingPlatform.Hal.Tests")]
