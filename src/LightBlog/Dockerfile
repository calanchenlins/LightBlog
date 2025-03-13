#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS runtime
# 设置容器内的工作目录为 `/app` 文件夹, 即容器启动默认的文件夹
WORKDIR /app
COPY . .
# 声明运行时容器提供服务端口, 需要通过 -p 设置绑定
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "LightBlog.dll"] 
