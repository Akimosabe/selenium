using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Text.Json;
using System.Linq;

namespace Stafftests;

public class Tests
{
    private IWebDriver _driver;
    private WebDriverWait _wait;
    private const string BaseUrl = "https://staff-testing.testkontur.ru";
    private (string user, string pass) LoadSecrets() // Не добавляю в гитигнор, чтобы показать куда прячу секреты
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\secret.json"));
        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return (data["user"], data["pass"]);
    }
    [SetUp]
    public void Setup()
    {
        _driver = new ChromeDriver();
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(7));
    }

    [TearDown]
    public void TearDown()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }

    private void Authorize()
    {
        var (username, password) = LoadSecrets();
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            throw new Exception("Логин и пароль не заданы");
        _driver.Navigate().GoToUrl(BaseUrl);
        _driver.FindElement(By.Id("Username")).SendKeys(username);
        _driver.FindElement(By.Id("Password")).SendKeys(password);
        _driver.FindElement(By.Name("button")).Click();
    }

    [Test]
    public void Auth()
    {
        Authorize();
        _wait.Until(ExpectedConditions.UrlContains("/news"));
        var cookies = _driver.Manage().Cookies.AllCookies; // логика такая, что редирект может поменяться, а вот куки точно будут передаваться, но редирект я тоже оставил
        foreach (var c in cookies)
        {
            Console.WriteLine($"{c.Name} = {c.Value}");
        }
        var hasSession = cookies.Any(c =>
            c.Name == "GCloud.Staff.Session" && !string.IsNullOrEmpty(c.Value)
        );
        Console.WriteLine("Полученные куки: " + hasSession);
        Assert.That(hasSession, "Куки не установились");
    }
}