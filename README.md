# TravelToCoolPlace
Travel Suggestion to Cool Place

## Releases
v1.0

## v1.0

## API Definition
### 1. GetAuthenticated
Purpose : Verify user and provide a "Access Token"</br>

API Endpoint : POST /GetAuthenticated </br></br>
>> Request : ``` https://localhost:7282/api/Weather/GetAuthenticated ``` </br></br>
[ Params ]</br>
username = raskin</br>
password = 123</br>

>> Response</br>
{</br>
    "accessToken": "",</br>
    "expires": ""</br>
}</br>

### 2. TemperatureForecasts
Purpose : Get the temperature forecasts of each districts for up to 7 days for 64 districts at 2pm</br>
API Endpoint : GET /TemperatureForecasts </br></br>
>> Request : ``` https://localhost:7282/api/Weather/TemperatureForecasts ``` </br></br>
[ Header ]</br>
Key = Authorization</br>
Value = accessToken</br>

>> Response</br>
{</br>
    "2023-05-29T14:00:00": [</br>
        {</br>
            "name": "Dhaka",</br>
            "date": "2023-05-29T14:00:00",</br>
            "temperature": 31</br>
        },</br>
        .................</br>

### 3. CoolestDistricts
Purpose : Get the coolest 10 districts based on the average temperature at 2pm for the next 7 days</br>
API Endpoint : GET /CoolestDistricts </br></br>
>> Request : ``` https://localhost:7282/api/Weather/CoolestDistricts ``` </br></br>
[ Header ]</br>
Key = Authorization</br>
Value = accessToken</br>

>> Response</br>
[</br>
    {</br>
        "districtName": "Bandarban",</br>
        "averageTemperature": 29.442857142857143</br>
    },</br>
    ............</br>

### 4. TravelSuggestion
Purpose : Compare the temperature of Friend's location and my preffered locations at 2 PM on a given day and return a response where should they travel</br>
API Endpoint : GET /TravelSuggestion </br></br>
>> Request : ``` https://localhost:7282/api/Weather/TravelSuggestion ``` </br></br>
[ Params ]</br>
friendLocation = </br>
destination = </br>
travelDate</br>

>> [ Header ]</br>
Key = Authorization</br>
Value = accessToken</br>

>> Response</br>
Your Location is Coolest, So You Should Travel to [coolest location] </br></br>

## Follow these steps to get started with this API.</br>

### Download Visual Studio 2022
Download and instal Visual Studio 2022 [Link](https://visualstudio.microsoft.com/vs/community/) </br></br>

### Git URL of the Project
Clone the project from this [Git Repo](https://github.com/raskin-soft/TravelToCoolPlace.git) then run the application from visual studio 2022 </br></br>

### Download the Extension
Right click on project and click Manage Nuget Packages then install below packages
1. Microsoft.AspNetCore.Mvc.Newtonsoftjson(6.0.16)
2. System.IdentityModel.Tokens.Jwt(6.30.1)
3. Swashbuckle.AspNetCore.Annotations(6.5.0) </br></br>

### Test the APIs
1. Run the application from Visual Studio
2. Test the APIs endpoints using Postman
3. OR, use swagger to test it. just go to here : https://localhost:7282/swagger


## About the Author
### Mohammad Raskinur Rashid
- Github [github.com/raskin-soft](https://github.com/raskin-soft)
- Linkedin - [raskinurrashid](https://www.linkedin.com/in/raskinurrashid/)