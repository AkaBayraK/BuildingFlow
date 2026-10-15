import {Platform} from 'react-native';
import * as Picker from 'expo-image-picker';
import {ImageManipulator,SaveFormat} from 'expo-image-manipulator';
import {File,Paths} from 'expo-file-system';
import {MAX_PHOTO_BYTES,base64Bytes} from './contracts';
export async function selectPhoto(camera:boolean):Promise<string|null>{
 if(camera){if(Platform.OS==='web')throw Error('Kamera için mobil uygulamayı kullanın.');const permission=await Picker.requestCameraPermissionsAsync();if(!permission.granted)throw Error('Kamera izni verilmedi. Galeriden seçim yapabilir veya fotoğraf eklemeden devam edebilirsiniz.');}
 const options={mediaTypes:['images'] as Picker.MediaType[],allowsEditing:true,aspect:[1,1] as [number,number],quality:1};
 const result=camera?await Picker.launchCameraAsync(options):await Picker.launchImageLibraryAsync(options);
 if(result.canceled)return null;const asset=result.assets[0];
 let output:string|undefined;
 try{
  if(!asset || !/^(file|content|blob|data):/.test(asset.uri)||asset.width<1||asset.height<1||asset.width*asset.height>40000000||(asset.fileSize??0)>20*1024*1024 || (asset.mimeType&&!['image/jpeg','image/png','image/heic','image/heif','image/webp'].includes(asset.mimeType)))throw Error('JPEG, PNG, HEIC veya WebP fotoğraf seçin; üst sınır 20 MB.');
  if(Platform.OS!=='web' && asset.uri.startsWith('file:') && new File(asset.uri).size>20*1024*1024)throw Error('Fotoğraf 20 MB sınırını aşıyor.');
  const side=Math.min(asset.width,asset.height);const context=ImageManipulator.manipulate(asset.uri);context.crop({originX:Math.floor((asset.width-side)/2),originY:Math.floor((asset.height-side)/2),width:side,height:side});context.resize({width:512,height:512});const image=await context.renderAsync();const saved=await image.saveAsync({format:SaveFormat.JPEG,compress:.75,base64:true});output=saved.uri;
  if(!saved.base64||base64Bytes(saved.base64).length>MAX_PHOTO_BYTES)throw Error('Fotoğraf sıkıştırıldıktan sonra 256 KB sınırını aşıyor. Daha sade bir fotoğraf seçin.');return saved.base64;
 }finally{if(Platform.OS!=='web')for(const uri of [output,asset?.uri])if(uri?.startsWith(Paths.cache.uri)){try{const file=new File(uri);if(file.exists)file.delete();}catch{/* Only app-owned temporary copies are eligible for removal. */}}}
}
