FROM microsoft/aspnetcore:2.0
COPY ./src/WebApplication/bin/Release/netcoreapp2.0/publish/ /app
WORKDIR /app
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebApplication.dll"]