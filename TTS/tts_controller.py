import argparse
import xml.etree.ElementTree as ET
from chatterbox.tts import ChatterboxTTS
import torchaudio
import os
import traceback  # For full error traces
import logging  # For file and console logging

def generate_tts(xml_path, output_dir, voice=None, voices_dir='voices', logger=None):
    try:
        logger.info("Starting TTS generation...")  # Debug: Entry point
        
        # Ensure output directory exists
        os.makedirs(output_dir, exist_ok=True)
        logger.info(f"Output directory: {output_dir}")
        
        # Parse XML
        logger.info("Parsing XML...")
        tree = ET.parse(xml_path)
        root = tree.getroot()
        logger.info(f"Found {len(list(root.findall('Question')))} questions in XML.")
        
        # Load TTS model (use CPU if no GPU)
        logger.info("Loading ChatterboxTTS model...")
        model = ChatterboxTTS.from_pretrained(device="cpu")  # Switch to "cuda" if GPU available
        logger.info("Model loaded successfully.")
        
        use_single = bool(voice)
        if not use_single:
            # Get unique lecturer IDs from XML, defaulting to "berninger" if missing
            lecturer_ids = set()
            for question in root.findall('Question'):
                lecturer_elem = question.find('LecturerID')
                if lecturer_elem is not None and lecturer_elem.text:
                    lecturer_ids.add(lecturer_elem.text)
                else:
                    logger.warning(f"Missing or empty LecturerID for a question; defaulting to 'berninger'.")
                    lecturer_ids.add("berninger")
            default_voice = os.path.join(voices_dir, "berninger.wav")
            logger.info(f"Unique Lecturer IDs (after defaults): {lecturer_ids}")
        else:
            lecturer_ids = [None]  # Single voice mode, no lecturer iteration
            logger.info("Using single voice mode.")
        
        # Process questions for each lecturer sequentially
        for lecturer_id in lecturer_ids:
            logger.info(f"Processing audio for lecturer: {lecturer_id if lecturer_id else 'single voice'}")
            voice_path = voice if use_single else os.path.join(voices_dir, f"{lecturer_id}.wav")
            if not use_single and not os.path.exists(voice_path):
                logger.warning(f"Voice sample not found for {lecturer_id} at {voice_path}. Using default.")
                voice_path = default_voice
            
            if not os.path.exists(voice_path):
                logger.warning(f"Voice sample not found at {voice_path}. Skipping.")
                continue
            
            logger.info(f"Using voice file: {voice_path}")
            
            # Process questions for this lecturer
            for idx, question in enumerate(root.findall('Question')):
                lecturer_elem = question.find('LecturerID')
                question_lecturer = lecturer_elem.text if lecturer_elem is not None else "berninger" if not use_single else None
                if not use_single and question_lecturer != lecturer_id:
                    continue  # Skip questions not matching the current lecturer
                tts_text_elem = question.find('TTSFriendlyText')
                tts_text = tts_text_elem.text if tts_text_elem is not None else None
                if tts_text:
                    logger.info(f"Generating audio for question {idx}: '{tts_text[:50]}...'")  # Truncate for log readability

                    # Generate audio with cloned voice
                    wav = model.generate(tts_text, audio_prompt_path=voice_path)
                    output_path = os.path.join(output_dir, f"question_{idx}.wav")
                    torchaudio.save(output_path, wav, model.sr)
                    logger.info(f"Generated audio for question {idx}: {output_path}")
                else:
                    logger.warning(f"No TTS text for question {idx}, skipping.")
        
        logger.info("TTS generation completed successfully!")
        
    except Exception as e:
        logger.exception(f"Error during TTS generation: {e}")  # Logs full traceback
        raise  # Re-raise to ensure process exits with error code

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument('--xml', required=True, help="Path to questions.xml")
    parser.add_argument('--output', required=True, help="Output directory for WAV files")
    parser.add_argument('--voice', required=False, help="Path to single voice sample WAV")
    parser.add_argument('--voices_dir', required=False, default='voices', help="Directory for lecturer voice samples")
    args = parser.parse_args()
    
    # Set up logging to AppData folder (console + file)
    log_dir = os.path.join(os.environ['APPDATA'], "DHBW-Game", "logs")
    os.makedirs(log_dir, exist_ok=True)
    log_path = os.path.join(log_dir, "tts_controller.log")
    
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(levelname)s - %(message)s',
        handlers=[
            logging.FileHandler(log_path, mode='a', encoding='utf-8'),  # Append to file in AppData
            logging.StreamHandler()  # Also output to console (Python window)
        ]
    )
    logger = logging.getLogger(__name__)
    logger.info(f"TTS Controller started. Log file: {log_path}")
    
    try:
        generate_tts(args.xml, args.output, args.voice, args.voices_dir, logger)
    except Exception as e:
        logger.exception(f"Fatal error: {e}")