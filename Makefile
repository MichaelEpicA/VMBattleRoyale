all: vm host server monitor
vm:
	dotnet build "VM Battle Royale VM/VM Battle Royale VM Setup.csproj"
host:
	dotnet build "VM Battle Royale Host/VM Battle Royale Host.csproj"
monitor:
	dotnet build "VM Battle Royale Monitor/VM Battle Royale Monitor.csproj"
server:
	dotnet build "VM Battle Royale Server/VM Battle Royale Server.csproj"
clean:
	rm -r */bin */obj
