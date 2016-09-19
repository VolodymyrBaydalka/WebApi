[![zvsharp MyGet Build Status](https://www.myget.org/BuildSource/Badge/zvsharp?identifier=e3950b12-4d7f-4043-b8f6-57903cd91713)](https://www.myget.org/)
WebApi
========
Yet another implementation of Retrofit for .NET based on RealProxy class

How To Use
========
  Step 1) Create interface for your API
  ```
  public interface IGitHub
  {
    JObject RateLimitJson();
  }
  ```
  
  Step 2) Add models for your API
  ```
  public class Limits
  {
      public int Limit { get; set; }
      public int Remaining { get; set; }
      public long Reset { get; set; }
  }
  
  public class RateLimits
  {
      public Limits Rate { get; set; }
      public Dictionary<string, Limits> Resources { get; set; }
  }
  ```
  Step 3) Add mapping attributes
  ```
  [Header("User-Agent", "WebApi")]
  public interface IGitHub
  {
      [Get("rate_limit")]
      RateLimits RateLimit();

      [Get("rate_limit")]
      JObject RateLimitJson();
  }
  ```
  
  Step 4) Use
  ```
    var client = new WebApiClient(https://api.github.com).Build<IGitHub>();
    var result = client.RateLimit();
  ```

License
=======

    Copyright 2016 Volodymyr Baydalka

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
