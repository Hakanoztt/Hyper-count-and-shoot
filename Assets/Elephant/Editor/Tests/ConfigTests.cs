using System.Collections.Generic;
using ElephantSDK;
using NUnit.Framework;

namespace Elephant.Editor.Tests
{
    public class ConfigTests
    {

        private static string configJsonTest =
            "{\"tag\":\"NO_TAG\",\"data\":{\"keys\":[\"stringKey\",\"intKey\",\"boolKey\",\"longKey\",\"doubleKey\",\"floatKey\"],\"values\":[\"string\",\"600\",\"true\",\"5000000\",\"0.1\",\"1.5\"]}}";
        
        
        [Test]
        public void GetFromRemoteConfig()
        {
            var remoteConfig = RemoteConfig.GetInstance();
            remoteConfig.Init(configJsonTest);
            
            Assert.That(remoteConfig.Get("stringKey", "key"), Is.EqualTo("string"));
            Assert.That(remoteConfig.GetInt("intKey", 0), Is.EqualTo(600));
            Assert.That(remoteConfig.GetBool("boolKey", false), Is.EqualTo(true));
            Assert.That(remoteConfig.GetLong("longKey", 0), Is.EqualTo(5000000));
            Assert.That(remoteConfig.GetFloat("floatKey", 0f), Is.EqualTo(1.5));
            Assert.That(remoteConfig.GetDouble("doubleKey", 0.0), Is.EqualTo(0.1));
            
            Assert.That(remoteConfig.Get("intKey", "key"), Is.EqualTo("600"));
            Assert.That(remoteConfig.GetInt("stringKey", 0), Is.EqualTo(0));
            Assert.That(remoteConfig.GetBool("longKey", false), Is.EqualTo(false));
            Assert.That(remoteConfig.GetLong("boolKey", 0), Is.EqualTo(0));
            Assert.That(remoteConfig.GetFloat("stringKey", 0f), Is.EqualTo(0f));
            Assert.That(remoteConfig.GetDouble("boolKey", 0.0), Is.EqualTo(0.0));
        }
        
        [Test]
        public void GetFromAdConfig()
        {
            var adConfig = AdConfig.GetInstance();
            
            List<AdConfigParameter> parameters = new List<AdConfigParameter>();
            AdConfigParameter param1 = new AdConfigParameter();
            AdConfigParameter param2 = new AdConfigParameter();
            AdConfigParameter param3 = new AdConfigParameter();
            AdConfigParameter param4 = new AdConfigParameter();
            param1.key = "stringKey";
            param1.value = "string";
            parameters.Add(param1);
            param2.key = "intKey";
            param2.value = "6000";
            parameters.Add(param2);
            param3.key = "boolKey";
            param3.value = "true";
            parameters.Add(param3);
            param4.key = "longKey";
            param4.value = "60000000";
            parameters.Add(param4);

            adConfig.parameters = parameters;
            
            adConfig.Init(adConfig);
            
            Assert.That(adConfig.Get("stringKey", "key"), Is.EqualTo("string"));
            Assert.That(adConfig.GetInt("intKey", 0), Is.EqualTo(6000));
            Assert.That(adConfig.GetBool("boolKey", false), Is.EqualTo(true));
            Assert.That(adConfig.GetLong("longKey", 0), Is.EqualTo(60000000));
            
            Assert.That(adConfig.Get("intKey", "key"), Is.EqualTo("6000"));
            Assert.That(adConfig.GetInt("stringKey", 0), Is.EqualTo(0));
            Assert.That(adConfig.GetBool("longKey", false), Is.EqualTo(false));
            Assert.That(adConfig.GetLong("boolKey", 0), Is.EqualTo(0));
        }

    }
}