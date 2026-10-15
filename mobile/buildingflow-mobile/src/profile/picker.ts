import {Platform} from 'react-native';
import * as Picker from 'expo-image-picker';
import {ImageManipulator,SaveFormat} from 'expo-image-manipulator';
import {File,Paths} from 'expo-file-system';
import {MAX_PHOTO_BYTES,base64Bytes} from './contracts';
type Asset=Picker.ImagePickerAsset;
async function pick(camera:boolean,square:boolean):Promise<Asset|null>{
 if(camera){if(Platform.OS==='web')throw Error('Kamera için mobil uygulamayı kullanın.');const permission=await Picker.requestCameraPermissionsAsync();if(!permission.granted)throw Error('Kamera izni verilmedi. Galeriden seçim yapabilir veya fotoğraf eklemeden devam edebilirsiniz.');}
 // Location/EXIF is not requested; the server additionally strips metadata from stored JPEGs.
 const options={mediaTypes:['images'] as Picker.MediaType[],allowsEditing:square,...(square?{aspect:[1,1] as [number,number]}:{}),quality:1,exif:false};
 const result=camera?await Picker.launchCameraAsync(options):await Picker.launchImageLibraryAsync(options);
 if(result.canceled)return null;
 const asset=result.assets[0];
 if(!asset || !/^(file|content|blob|data):/.test(asset.uri)||asset.width<1||asset.height<1||asset.width*asset.height>40000000||(asset.fileSize??0)>20*1024*1024 || (asset.mimeType&&!['image/jpeg','image/png','image/heic','image/heif','image/webp'].includes(asset.mimeType)))throw Error('JPEG, PNG, HEIC veya WebP fotoğraf seçin; üst sınır 20 MB.');
 if(Platform.OS!=='web' && asset.uri.startsWith('file:') && new File(asset.uri).size>20*1024*1024)throw Error('Fotoğraf 20 MB sınırını aşıyor.');
 return asset;
}
function cleanup(uris:(string|undefined)[]){if(Platform.OS!=='web')for(const uri of uris)if(uri?.startsWith(Paths.cache.uri)){try{const file=new File(uri);if(file.exists)file.delete();}catch{/* Only app-owned temporary copies are eligible for removal. */}}}
export async function selectPhoto(camera:boolean):Promise<string|null>{
 const asset=await pick(camera,true);if(!asset)return null;
 let output:string|undefined;
 try{
  const side=Math.min(asset.width,asset.height);const context=ImageManipulator.manipulate(asset.uri);context.crop({originX:Math.floor((asset.width-side)/2),originY:Math.floor((asset.height-side)/2),width:side,height:side});context.resize({width:512,height:512});const image=await context.renderAsync();const saved=await image.saveAsync({format:SaveFormat.JPEG,compress:.75,base64:true});output=saved.uri;
  if(!saved.base64||base64Bytes(saved.base64).length>MAX_PHOTO_BYTES)throw Error('Fotoğraf sıkıştırıldıktan sonra 256 KB sınırını aşıyor. Daha sade bir fotoğraf seçin.');return saved.base64;
 }finally{cleanup([output,asset.uri]);}
}
// Request evidence keeps the original aspect ratio; size steps down until the shared 256 KB server limit fits.
export async function selectRequestPhoto(camera:boolean):Promise<string|null>{
 const asset=await pick(camera,false);if(!asset)return null;
 const outputs:string[]=[];
 try{
  for(const [edge,compress] of [[1280,.7],[1280,.5],[960,.5],[720,.45]] as const){
   const scale=Math.min(1,edge/Math.max(asset.width,asset.height));const context=ImageManipulator.manipulate(asset.uri);context.resize({width:Math.max(1,Math.round(asset.width*scale)),height:Math.max(1,Math.round(asset.height*scale))});
   const saved=await (await context.renderAsync()).saveAsync({format:SaveFormat.JPEG,compress,base64:true});outputs.push(saved.uri);
   if(saved.base64 && base64Bytes(saved.base64).length<=MAX_PHOTO_BYTES)return saved.base64;
  }
  throw Error('Fotoğraf 256 KB sınırına sıkıştırılamadı. Daha sade bir fotoğraf seçin.');
 }finally{cleanup([...outputs,asset.uri]);}
}
