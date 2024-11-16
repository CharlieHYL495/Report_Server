
# Telerik Report API Server

### Tasks 
1. [ ] **Periodic Get Data From Reporting Server:** 
    - [ ] **Period can be changed:** Period should be a parameter in appsettings.json
    - [ ] **Store reports in file:** Store all report files in a proper folder which can be accessed from web
    - [ ] **Store reports in Redis:** Store all jsons in Redis using servicestack.redis
```shell
# 登录
curl --location 'https://reporting.wyocrm.com:4443/token' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data-urlencode 'grant_type=password' \
--data-urlencode 'username=wyo' \
--data-urlencode 'password=VBClZ6DlCOk0f8Gi00AY8aSD0N7D3JdFJpjitGKA'

# Get All Categories
curl --location 'https://reporting.wyocrm.com:4443/api/reportserver/v2/categories' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--header 'Authorization: Bearer HtWBXpyRGhiUrPuIIMnxOT5HF36wgHHrLq3bxufJ7Qz5nHsHx1p9eEGVwxDcrms9NCBdvh4wIWzET7XmqGQbyh8Jvr8Z2atPdXi9SFwBsSjqVwiAi_R_B-arKYTzd7vjIyA5lsZSSWid1acllKRAMjyDecFZ5Oy6DmA12kwYtWqotfhFA-1Rz20xIKG5vSUlamVN0KSNcDpU5Jzxev65cbHQmdy7JmVXI_B9zTezd75a2YZ7QD6euYRFVNr-xkzyoFCRlAkrgFrmk8xb8uCuBq7B5wWalkJkg6QB9grL3NIGK1N7gdIi_N943wOcEVw3gZIHiLXS42HdoQqPrmPnlgvHI85As94QRl0NRAlH36wzGBpNQdN8475u6WdWWxeIEGxu6mgvCjxS3yIPvCqRUSmwqc0Vx5C3i3Nez4j4geXGksyaBtIGTJfFMYa7WT0FKOabfKZWR3zTj9QQfUtRtiQHSSne-rJb_nbt4woMetV3NuqMVYE-kv6TqVcgI6X9e6Y5ZoLN2uCRie7Vw9Yc7A'

# Get All Reports By A Category
curl --location 'https://reporting.wyocrm.com:4443/api/reportserver/v2/categories/a8823e6428f/reports' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--header 'Authorization: Bearer HtWBXpyRGhiUrPuIIMnxOT5HF36wgHHrLq3bxufJ7Qz5nHsHx1p9eEGVwxDcrms9NCBdvh4wIWzET7XmqGQbyh8Jvr8Z2atPdXi9SFwBsSjqVwiAi_R_B-arKYTzd7vjIyA5lsZSSWid1acllKRAMjyDecFZ5Oy6DmA12kwYtWqotfhFA-1Rz20xIKG5vSUlamVN0KSNcDpU5Jzxev65cbHQmdy7JmVXI_B9zTezd75a2YZ7QD6euYRFVNr-xkzyoFCRlAkrgFrmk8xb8uCuBq7B5wWalkJkg6QB9grL3NIGK1N7gdIi_N943wOcEVw3gZIHiLXS42HdoQqPrmPnlgvHI85As94QRl0NRAlH36wzGBpNQdN8475u6WdWWxeIEGxu6mgvCjxS3yIPvCqRUSmwqc0Vx5C3i3Nez4j4geXGksyaBtIGTJfFMYa7WT0FKOabfKZWR3zTj9QQfUtRtiQHSSne-rJb_nbt4woMetV3NuqMVYE-kv6TqVcgI6X9e6Y5ZoLN2uCRie7Vw9Yc7A'

# Get Report Latest Revision
curl --location 'https://reporting.wyocrm.com:4443/api/reportserver/v2/reports/09cb7f6a038/revisions/latest' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--header 'Authorization: Bearer HtWBXpyRGhiUrPuIIMnxOT5HF36wgHHrLq3bxufJ7Qz5nHsHx1p9eEGVwxDcrms9NCBdvh4wIWzET7XmqGQbyh8Jvr8Z2atPdXi9SFwBsSjqVwiAi_R_B-arKYTzd7vjIyA5lsZSSWid1acllKRAMjyDecFZ5Oy6DmA12kwYtWqotfhFA-1Rz20xIKG5vSUlamVN0KSNcDpU5Jzxev65cbHQmdy7JmVXI_B9zTezd75a2YZ7QD6euYRFVNr-xkzyoFCRlAkrgFrmk8xb8uCuBq7B5wWalkJkg6QB9grL3NIGK1N7gdIi_N943wOcEVw3gZIHiLXS42HdoQqPrmPnlgvHI85As94QRl0NRAlH36wzGBpNQdN8475u6WdWWxeIEGxu6mgvCjxS3yIPvCqRUSmwqc0Vx5C3i3Nez4j4geXGksyaBtIGTJfFMYa7WT0FKOabfKZWR3zTj9QQfUtRtiQHSSne-rJb_nbt4woMetV3NuqMVYE-kv6TqVcgI6X9e6Y5ZoLN2uCRie7Vw9Yc7A'

# Get Report Parameters
curl --location 'https://reporting.wyocrm.com:4443/api/reportserver/v2/reports/09cb7f6a038/parameters' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--header 'Authorization: Bearer HtWBXpyRGhiUrPuIIMnxOT5HF36wgHHrLq3bxufJ7Qz5nHsHx1p9eEGVwxDcrms9NCBdvh4wIWzET7XmqGQbyh8Jvr8Z2atPdXi9SFwBsSjqVwiAi_R_B-arKYTzd7vjIyA5lsZSSWid1acllKRAMjyDecFZ5Oy6DmA12kwYtWqotfhFA-1Rz20xIKG5vSUlamVN0KSNcDpU5Jzxev65cbHQmdy7JmVXI_B9zTezd75a2YZ7QD6euYRFVNr-xkzyoFCRlAkrgFrmk8xb8uCuBq7B5wWalkJkg6QB9grL3NIGK1N7gdIi_N943wOcEVw3gZIHiLXS42HdoQqPrmPnlgvHI85As94QRl0NRAlH36wzGBpNQdN8475u6WdWWxeIEGxu6mgvCjxS3yIPvCqRUSmwqc0Vx5C3i3Nez4j4geXGksyaBtIGTJfFMYa7WT0FKOabfKZWR3zTj9QQfUtRtiQHSSne-rJb_nbt4woMetV3NuqMVYE-kv6TqVcgI6X9e6Y5ZoLN2uCRie7Vw9Yc7A'

```



This project implements a server-side API for Telerik Reporting, allowing you to generate and manage reports programmatically.

## Prerequisites

- .NET Core SDK (Version 6.0)
- Telerik Reporting license
- ServiceStack ORM (for storing report definitions and data, if applicable)

## Getting Started

1. Clone this repository:
   ```
   git clone https://github.com/yourusername/telerik-report-api.git
   ```

2. Navigate to the project directory:
   ```
   cd telerik-report-api
   ```

3. Restore the NuGet packages:
   ```
   dotnet restore
   ```

4. Update the `appsettings.json` file with your database connection string and Telerik Reporting license key.

5. Run the application:
   ```
   dotnet run
   ```

## Project Structure

- `Controllers/`: Contains API controllers for handling report-related requests
- `Models/`: Defines data models used in the application
- `Services/`: Implements business logic and report generation services
- `Startup.cs`: Configures the application and services

## API Endpoints

- `GET /api/reports`: Retrieve a list of available reports
- `GET /api/reports/{id}`: Get details of a specific report
- `POST /api/reports/generate`: Generate a report based on provided parameters
- `GET /api/reports/download/{id}`: Download a generated report

## Configuration

In the `appsettings.json` file, you can configure:

- Database connection string
- Telerik Reporting license key
- Logging settings
- Other application-specific settings

## Development

1. Implement new report templates in the Telerik Report Designer.
2. Add new API endpoints in the `ReportsController.cs` file as needed.
3. Implement corresponding services in the `Services/` directory.

## Testing

Run unit tests using:
```
dotnet test
```

## Deployment

1. Publish the application:
   ```
   dotnet publish -c Release
   ```

2. Deploy the published files to your hosting environment.

3. Ensure your hosting environment has the necessary dependencies installed.

## Troubleshooting

- If you encounter issues with report generation, check the Telerik Reporting log files.
- For API-related issues, review the application logs in the `logs/` directory.

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.