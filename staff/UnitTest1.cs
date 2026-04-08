using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Text.Json;

namespace Stafftests;

public class Tests
{
    private IWebDriver driver;
    private WebDriverWait wait;
    private const string StaffUrl = "https://staff-testing.testkontur.ru";
    private (string user, string pass) LoadSecrets() // Не добавляю в гитигнор, чтобы показать куда прячу секреты
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\secret.json"));
        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return (data["user"], data["pass"]);
    }
        private void Auth() //перенес выше ближе к лоадсекрет, как вспомогательный метод, а не под атрибуты
    {
        var (username, password) = LoadSecrets();
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            throw new Exception("Логин и пароль не заданы");
        driver.Navigate().GoToUrl(StaffUrl);
        driver.FindElement(By.Id("Username")).SendKeys(username);
        driver.FindElement(By.Id("Password")).SendKeys(password);
        driver.FindElement(By.Name("button")).Click();
    }
    private void NewsWaiter() // выделил отдельно, логика следующая: если редирект после после входа меняется на другую страницу, просто меняем этот код и все
    {
        wait.Until(ExpectedConditions.UrlContains("/news"));
    }

    [SetUp]
    public void Setup()
    {
        driver = new ChromeDriver();
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
        wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
    }

    [TearDown]
    public void TearDown()
    {
        driver?.Quit();
        driver?.Dispose();
    }

    [Test]
    public void Login()
    {
        Auth();
        NewsWaiter();
        var cookies = driver.Manage().Cookies.AllCookies; // исходил из логики, что редирект может поменяться, а вот куки точно будут передаваться, но редирект я тоже оставил
        foreach (var c in cookies)
        {
            Console.WriteLine($"{c.Name} = {c.Value}");
        }
        var hasSession = cookies.Any(c =>
            c.Name == "GCloud.Staff.Session" && !string.IsNullOrEmpty(c.Value)
        );
        Console.WriteLine("Полученные куки: " + hasSession); //проверял как работает
        
        Assert.That(driver.Url, Does.Contain("/news"), "После авторизации не произошёл редирект на /news");
        Assert.That(hasSession, "Куки не установились");
    }
    [Test]
    public void Logout()
    {
        Auth();
        NewsWaiter(); 
        var sidebarMenuButton = wait.Until(ExpectedConditions.ElementToBeClickable(
            By.CssSelector("[data-tid='SidebarMenuButton']")));
        sidebarMenuButton.Click();
        var logoutButton = wait.Until(ExpectedConditions.ElementToBeClickable(
            By.CssSelector("[data-tid='LogoutButton']")));
        logoutButton.Click();
        wait.Until(ExpectedConditions.UrlContains("/Account/Logout"));
        Assert.That(driver.Url, Does.Contain("/Account/Logout"), "После выхода не произошёл редирект на /Account/Logout");
        
    }
    
    [Test]
    public void SearchEmployee()
    {
        driver.Manage().Window.Maximize(); 

        // при открытии ChromeDriver без указания размера окна SearchBar прячется за data-tid="Services"
        // но обычном браузере SearchBar всегда доступен при любом разрешении
        // поэтому для данного теста использовал Maximize, хотя, конечно можно было кликнуть сначала на Services
        // вот так:
        // var servicesButton = wait.Until(ExpectedConditions.ElementToBeClickable(
        //     By.CssSelector("[data-tid='Services']")));
        // servicesButton.Click();

        Auth();
        NewsWaiter();
        

        var searchBar = wait.Until(ExpectedConditions.ElementToBeClickable(
            By.CssSelector("[data-tid='SearchBar']")));
        searchBar.Click();
        
        var searchInput = wait.Until(ExpectedConditions.ElementExists(
            By.CssSelector("[data-tid='SearchBar'] input")));
        var (username, _) = LoadSecrets();
    searchInput.SendKeys(username); // ну, этот точно есть
        
        var suggestion = wait.Until(ExpectedConditions.ElementIsVisible(
            By.CssSelector("[data-tid='ComboBoxMenu__item']")));
        Assert.That(suggestion.Displayed, Is.True,
            "После ввода не появилась подсказка с сотрудником");
    }
    [Test]
    public void UploadFile()
    {
        Auth();
        NewsWaiter();
        
        driver.Navigate().GoToUrl(StaffUrl + "/files");
        
        var addButton = wait.Until(ExpectedConditions.ElementToBeClickable(
            By.XPath("//button[.//span[text()='Добавить']]"))); //нет data-tid
        addButton.Click();
        
        // пришлось вспоминать показанный на занятии по devtools js-код и писать его в консоли браузера, чтобы проинспектировать элемент:
        // setTimeout(() => { debugger; }, 3000)

        var fileMenuItem = wait.Until(ExpectedConditions.ElementToBeClickable(
            By.XPath("//*[text()='Файл']")));
        fileMenuItem.Click();
        
                var fileInput = wait.Until(ExpectedConditions.ElementExists(
            By.CssSelector("input[type='file']")));
        var filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\media\test_cow.jpg"));
        fileInput.SendKeys(filePath);
        
        var uploadedFile = wait.Until(ExpectedConditions.ElementIsVisible(
            By.XPath("//*[contains(text(),'test_cow')]")));
        Assert.That(uploadedFile.Displayed, Is.True,
            "Загруженный файл не появился в списке файлов");
    }
}