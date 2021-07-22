CSC=csc
GOC=go
GOF=GOOS=windows GOARCH=amd64
GBCMD=$(GOF) $(GOC) build -o bin/$(shell find src/$@ $< -type f -iregex ".*\.go" | sed -e 's/\.go/\.exe/g' -e 's/src\///g') \
$(shell find src/$@ $< -type f -iregex ".*\.go")
CSBCMD=$(CSC) -out:bin/$(shell find src/$@ $< -type f -iregex ".*\.cs" | sed -e 's/\.cs/\.exe/g' -e 's/src\///g') \
$(shell find src/$@ $< -type f -iregex ".*\.cs")
all: vm host
vm:
	mkdir -p bin/$@ $<
	$(GBCMD)
	$(CSBCMD)
host:
	mkdir -p bin/$@ $<
	$(CSBCMD)
clean:
	rm -r bin