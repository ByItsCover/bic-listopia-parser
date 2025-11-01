from PIL import Image
import io
import torch
from torch import Tensor
import clip

import json
import asyncio
import aiohttp
from aiohttp import ClientSession

from typing import List
from litserve import Request
import litserve as ls


class ClipServer(ls.LitAPI):
    def setup(self, device):
        self.model_name = "ViT-B/32"
        self.device = device
        self.clip_model, self.preprocess = clip.load(self.model_name, device=self.device)
    
    def decode_request(self, request: Request):
        image_urls: List[str | None] = request["image_urls"]
        images_tensor, was_processed = asyncio.run(self._retrieve_images(image_urls))
        
        return (images_tensor, was_processed)
    
    async def _retrieve_images(self, urls: List[str | None]):
        async with aiohttp.ClientSession() as session:
            ret = await asyncio.gather(*(self._get_image(url, session) for url in urls))
        
        processed_images = []
        was_processed = []
        for image in ret:
            if image is None:
                was_processed.append(False)
            else:
                processed_images.append(image)
                was_processed.append(True)
        
        print(f"Got {len(processed_images)} images")
        #print(f"First image: {processed_images[0]}")
        return torch.cat(processed_images, dim=0), was_processed
    
    async def _get_image(self, url: str | None, session: ClientSession):
        try:
            if url is None:
                return None
            async with session.get(url=url) as response:
                res = await response.read()
                image = self.preprocess(Image.open(io.BytesIO(res))).unsqueeze(0).to(self.device)
                
                return image
        except Exception as e:
            print(f"Unable to process image url {url} due to {e.__class__}.")
            return None
    
    def predict(self, processed_images: tuple[Tensor, List[bool]]):
        images_tensor, was_processed = processed_images
        with torch.no_grad():
            processed_embeddings = self.clip_model.encode_image(images_tensor).cpu().numpy()
        
        processed_embeddings_list = processed_embeddings.tolist()
        image_embeddings = []
        ind = 0
        for processed in was_processed:
            if processed:
                image_embeddings.append(processed_embeddings_list[ind])
                ind += 1
            else:
                image_embeddings.append(None)
        
        return {
                "image_embeddings": image_embeddings
            }


if __name__ == "__main__":
    server = ls.LitServer(ClipServer())
    server.run(port=8000, generate_client_file=False)
