# CachePwn
A tool to decrypt and extract data stored in PhatAC's cache.bin storage file

### Usage
CachePwn can be used to unpack and pack cache.bin files, see the following examples on how to achieve this.  

#### Unpack
Parameter 1: Mode (1 = Unpack).  
Parameter 2: Path to PhatAC's cache storage file. 

```
CachePwn.exe 1 C:\PhatAC\Data\cache.bin
```  

*This will generate 9 chunk files (\*.raw) and keys.json which contains the stage 1 decryption keys for the chunks.*

#### Pack
Parameter 1: Mode (2 = Pack).  
Parameter 2: Path to directory that contains the raw chunk files (*.raw) and keys.json.  

```
CachePwn.exe 2 C:\CachePwn
```

*This will generate a new cache.bin file using the supplied chunks and keys.*
