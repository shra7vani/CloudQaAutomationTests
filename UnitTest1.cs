using System;
using System.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace CloudQaAutomationTests
{
    [TestFixture]
    public class Tests
    {
        private IWebDriver _driver;
        private WebDriverWait _wait;
        private const string Url = "https://app.cloudqa.io/home/AutomationPracticeForm";

        [SetUp]
        public void Setup()
        {
            _driver = new ChromeDriver();
            _driver.Manage().Window.Maximize();
            _driver.Navigate().GoToUrl(Url);

            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        }

        [TearDown]
        public void TearDown()
        {
            _driver?.Dispose();
        }

        // ---------- helper methods (robust locators) ----------

        // Text input based on its label (e.g. "First Name")
        private IWebElement FindInputByLabel(string labelText)
        {
            var label = _wait.Until(d =>
                d.FindElement(By.XPath($"//label[normalize-space()='{labelText}']"))
            );

            return label.FindElement(By.XPath(".//following::input[1]"));
        }

        // Radio button based on label text or value (e.g. "Male")
        private IWebElement FindRadioByLabel(string labelText)
        {
            try
            {
                // label that CONTAINS the text (handles "Male :", "Male *", etc.)
                var label = _wait.Until(d =>
                    d.FindElement(By.XPath($"//label[contains(normalize-space(), '{labelText}')]"))
                );

                try
                {
                    // radio just before the label
                    return label.FindElement(By.XPath(".//preceding::input[@type='radio'][1]"));
                }
                catch (NoSuchElementException)
                {
                    // or just after the label
                    return label.FindElement(By.XPath(".//following::input[@type='radio'][1]"));
                }
            }
            catch (WebDriverTimeoutException)
            {
                // Fallback: locate radio by its value attribute (e.g. value="Male")
                return _wait.Until(d =>
                    d.FindElement(By.XPath($"//input[@type='radio' and contains(@value, '{labelText}')]"))
                );
            }
        }

        // Dropdown: find the <select> that contains an <option> with the given text (e.g. "India")
        // This does NOT rely on id/name/class or position, only visible option text.
        private SelectElement FindSelectHavingOption(string optionText)
        {
            var selectElement = _wait.Until(d =>
                d.FindElement(By.XPath(
                    $"//select[.//option[normalize-space()='{optionText}']]"
                ))
            );

            return new SelectElement(selectElement);
        }

        // ---------- TESTS FOR THREE FIELDS ----------

        [Test]
        public void FirstName_Field_CanBeFilled()
        {
            var firstNameInput = FindInputByLabel("First Name");

            const string testName = "TestUser_123";
            firstNameInput.Clear();
            firstNameInput.SendKeys(testName);

            Assert.That(
                firstNameInput.GetAttribute("value"),
                Is.EqualTo(testName),
                "First Name input did not contain the expected text."
            );
        }

        [Test]
        public void Gender_Male_CanBeSelected()
        {
            var maleRadio = FindRadioByLabel("Male");

            if (!maleRadio.Selected)
            {
                maleRadio.Click();
            }

            Assert.That(
                maleRadio.Selected,
                Is.True,
                "Male gender option should be selected."
            );
        }

        [Test]
        public void Country_Dropdown_Contains_India()
        {
            // find the dropdown that has "India" as an option
            var countrySelect = FindSelectHavingOption("India");

            // wait until dropdown has options (should be true already, but just in case)
            _wait.Until(d => countrySelect.Options.Count > 0);

            // verify that "India" is indeed one of the options
            var hasIndia = countrySelect.Options
                .Any(o => o.Text.Trim()
                    .Equals("India", StringComparison.OrdinalIgnoreCase));

            Assert.That(
                hasIndia,
                Is.True,
                "Country dropdown should contain India as one of the options."
            );
        }
    }
}
